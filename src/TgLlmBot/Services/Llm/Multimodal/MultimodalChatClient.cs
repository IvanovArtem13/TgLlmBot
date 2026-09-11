using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using TgLlmBot.Services.Media;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace TgLlmBot.Services.Llm.Multimodal;

/// <summary>
///     Встраивает вложения (<see cref="AttachedMediaContent" />) в тело запроса к LLM.
/// </summary>
/// <remarks>
///     <para>
///         Части <c>image_url</c> и <c>video_url</c> не выражаются средствами Microsoft.Extensions.AI:
///         коннектор OpenAI неизвестные ему контенты выбрасывает молча, а видео не умеет вовсе.
///         Поэтому на каждый раунд запроса (при tool calls их бывает несколько) этот клиент
///         подменяет массив <c>$.messages</c> целиком: конвертация сообщений в формат провайдера
///         и медиа-части дописываются здесь. Массив подменяется JsonPatch-ем через
///         <see cref="ChatOptions.RawRepresentationFactory" /> - точечный патч по индексу внутри
///         <c>$.messages</c> затирает остальной массив, заменять можно только целиком.
///     </para>
///     <para>
///         Клиент обязан стоять в пайплайне ниже <see cref="FunctionInvokingChatClient" />: тот
///         на каждом раунде дополняет список сообщений вызовами инструментов и их результатами,
///         и фабрика должна сериализовывать актуальный список, а не тот, с которого начали.
///         Вниз по пайплайну маркеры не идут - их место занимают короткие текстовые заглушки,
///         чтобы логи не тонули в base64 (тело запроса всё равно целиком заменяет патч).
///     </para>
/// </remarks>
public sealed class MultimodalChatClient : DelegatingChatClient
{
    private static readonly JsonSerializerOptions SerializationOptions = new(JsonSerializerDefaults.General)
    {
        // Кириллицу в промпте не экранируем: и в логах читаемо, и тело запроса меньше
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private readonly bool _disableThinking;

    public MultimodalChatClient(IChatClient innerClient, bool disableThinking)
        : base(innerClient)
    {
        _disableThinking = disableThinking;
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        var roundMessages = messages.ToList();
        if (!roundMessages.Any(static message => message.Contents.Any(static content => content is AttachedMediaContent)))
        {
            return base.GetResponseAsync(roundMessages, options, cancellationToken);
        }

        // Список раунда копируется в замыкание: фабрика вызывается коннектором по одному разу
        // на каждый запрос, и каждый раз должна сериализовывать именно этот раунд
        options ??= new ChatOptions();
        var disableThinking = _disableThinking;
        options.RawRepresentationFactory = _ => CreatePatchedOptions(roundMessages, disableThinking);
        return base.GetResponseAsync(ReplaceMarkersWithPlaceholders(roundMessages), options, cancellationToken);
    }

    /// <summary>
    ///     Стриминг с медиа-вложениями не поддерживается: подменить тело запроса по раундам
    ///     стрима нельзя, и молча потерять вложения было бы хуже, чем отказать сразу.
    /// </summary>
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Any(static message => message.Contents.Any(static content => content is AttachedMediaContent)))
        {
            throw new NotSupportedException("Streaming requests with attached media are not supported.");
        }

        return base.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    private static ChatCompletionOptions CreatePatchedOptions(IReadOnlyList<ChatMessage> roundMessages, bool disableThinking)
    {
        var messagesJson = SerializeRoundMessages(roundMessages);
        var videoMediaIoKwargsJson = BuildVideoMediaIoKwargs(roundMessages);
        var options = new ChatCompletionOptions();
#pragma warning disable SCME0001 // JsonPatch is for evaluation purposes only and is subject to change
        options.Patch.Set("$.messages"u8, messagesJson);
        if (videoMediaIoKwargsJson is not null)
        {
            options.Patch.Set("$.media_io_kwargs.video"u8, videoMediaIoKwargsJson);
        }

        if (disableThinking)
        {
            // Рассуждения выключены: описание вложения нужно целиком, а не в виде обрубленного
            // по лимиту токенов внутреннего монолога модели
            options.Patch.Set("$.chat_template_kwargs.enable_thinking"u8, false);
        }
#pragma warning restore SCME0001
        return options;
    }

    /// <summary>
    ///     Сообщения раунда в формате провайдера: то же, что сериализовал бы сам коннектор,
    ///     плюс медиа-части <c>image_url</c>/<c>video_url</c> на месте маркеров.
    /// </summary>
    private static byte[] SerializeRoundMessages(IReadOnlyList<ChatMessage> messages)
    {
        var result = new List<object>(messages.Count);
        foreach (var message in messages)
        {
            var role = message.Role.Value switch
            {
                "assistant" => "assistant",
                "system" => "system",
                "tool" => "tool",
                _ => "user"
            };
            var text = string.Concat(message.Contents.OfType<TextContent>().Select(static content => content.Text));
            var media = message.Contents.OfType<AttachedMediaContent>()
                .Select(static content => content.Media)
                .ToList();
            var toolCalls = message.Contents.OfType<FunctionCallContent>()
                .Select(static call => new
                {
                    id = call.CallId,
                    type = "function",
                    function = new
                    {
                        name = call.Name,
                        arguments = JsonSerializer.Serialize(call.Arguments, SerializationOptions)
                    }
                })
                .Cast<object>()
                .ToList();

            // Результаты инструментов живут в отдельных tool-сообщениях - как их сериализует
            // сам коннектор: строка уезжает как есть, всё прочее - JSON-ом
            foreach (var functionResult in message.Contents.OfType<FunctionResultContent>())
            {
                result.Add(new Dictionary<string, object?>
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = functionResult.CallId,
                    ["content"] = functionResult.Result is string resultText
                        ? resultText
                        : JsonSerializer.Serialize(functionResult.Result, SerializationOptions)
                });
            }

            if (message.Contents.OfType<FunctionResultContent>().Any()
                && text.Length is 0
                && toolCalls.Count is 0
                && media.Count is 0)
            {
                continue;
            }

            if (role is "assistant" && toolCalls.Count > 0)
            {
                var assistantMessage = new Dictionary<string, object?>
                {
                    ["role"] = role,
                    ["tool_calls"] = toolCalls
                };
                if (text.Length > 0)
                {
                    assistantMessage["content"] = text;
                }

                result.Add(assistantMessage);
                continue;
            }

            if (media.Count > 0)
            {
                // Медиа-части идут перед текстом: как видео, так и картинку модель должна
                // увидеть раньше, чем текст о нём
                var parts = new List<object>(media.Count + 1);
                parts.AddRange(media.Select(BuildMediaPart));
                parts.Add(new { type = "text", text });
                result.Add(new Dictionary<string, object?>
                {
                    ["role"] = role,
                    ["content"] = parts
                });
                continue;
            }

            result.Add(new Dictionary<string, object?>
            {
                ["role"] = role,
                ["content"] = text
            });
        }

        return JsonSerializer.SerializeToUtf8Bytes(result, SerializationOptions);
    }

    /// <summary>
    ///     Часть сообщения с вложением: цепочка кадров отправляется тем же способом, что и
    ///     видео, - data-url вида <c>data:video/jpeg;base64,кадр1,кадр2,...</c>, - так её
    ///     понимает vLLM, а модель видит обычное видео.
    /// </summary>
    private static object BuildMediaPart(PreparedMedia media)
    {
        return media.Kind is PreparedMediaKind.Image
            ? new
            {
                type = "image_url",
                image_url = new { url = media.DataUrl }
            }
            : new
            {
                type = "video_url",
                video_url = new { url = media.DataUrl }
            };
    }

    /// <summary>
    ///     Метаданные отправляемых кадров для <c>media_io_kwargs.video</c> либо
    ///     <see langword="null" />, если кадры не отправляются.
    /// </summary>
    /// <remarks>
    ///     Из них vLLM считает метки времени вида mm:ss, которые подставляет в промпт перед
    ///     каждым кадром. Без метаданных он берёт fps = 1, и трёхсекундная петля стикера
    ///     растянется для модели на шестнадцать секунд. Поле одно на весь запрос, поэтому
    ///     при нескольких отрендеренных анимациях тайминг берётся от первой: точные метки
    ///     времени нужны только стикерам, а стикер в одном сообщении бывает максимум один.
    /// </remarks>
    private static byte[]? BuildVideoMediaIoKwargs(IReadOnlyList<ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            foreach (var content in message.Contents.OfType<AttachedMediaContent>())
            {
                var animation = content.Media.Animation;
                if (content.Media.Kind is not PreparedMediaKind.RenderedFrames || animation is null)
                {
                    continue;
                }

                var mediaIoKwargs = new
                {
                    fps = animation.SourceFps,
                    frames_indices = animation.SourceFrameIndices,
                    total_num_frames = animation.SourceFrameCount,
                    duration = animation.SourceDuration.TotalSeconds,
                    num_frames = animation.Frames.Length
                };
                return JsonSerializer.SerializeToUtf8Bytes(mediaIoKwargs, SerializationOptions);
            }
        }

        return null;
    }

    /// <summary>
    ///     Копия сообщений раунда без маркеров: вниз по пайплайну (логирование, коннектор)
    ///     base64 носителей не нужен - тело запроса целиком заменяет патч из фабрики.
    /// </summary>
    private static List<ChatMessage> ReplaceMarkersWithPlaceholders(List<ChatMessage> messages)
    {
        var result = new List<ChatMessage>(messages.Count);
        foreach (var message in messages)
        {
            if (!message.Contents.Any(static content => content is AttachedMediaContent))
            {
                result.Add(message);
                continue;
            }

            var contents = new List<AIContent>(message.Contents.Count);
            foreach (var content in message.Contents)
            {
                if (content is AttachedMediaContent attached)
                {
                    contents.Add(new TextContent($"[медиа-вложение: {DescribePlaceholderKind(attached.Media)}, {attached.Media.PayloadBytes.ToString(System.Globalization.CultureInfo.InvariantCulture)} байт]"));
                }
                else
                {
                    contents.Add(content);
                }
            }

            result.Add(new ChatMessage(message.Role, contents));
        }

        return result;
    }

    private static string DescribePlaceholderKind(PreparedMedia media)
    {
        return media.Kind switch
        {
            PreparedMediaKind.Image => "картинка",
            PreparedMediaKind.VideoFile => "видео",
            PreparedMediaKind.RenderedFrames => "анимация",
            _ => "вложение"
        };
    }
}
