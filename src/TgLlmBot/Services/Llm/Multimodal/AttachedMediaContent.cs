using Microsoft.Extensions.AI;
using TgLlmBot.Services.Media;

namespace TgLlmBot.Services.Llm.Multimodal;

/// <summary>
///     Маркер вложения, приложенного к сообщению для LLM.
/// </summary>
/// <remarks>
///     Сам носитель в контент не кладётся, чтобы логи пайплайна не тонули в мегабайтах base64.
///     По маркерам <see cref="MultimodalChatClient" /> подменяет тело запроса: коннектор
///     Microsoft.Extensions.AI про неизвестные ему контенты молча выбрасывает, а видео
///     (<c>video_url</c>) не умеет вовсе - поэтому части <c>image_url</c>/<c>video_url</c>
///     дописываются в запрос напрямую, поверх сгенерированного SDK тела.
/// </remarks>
public sealed class AttachedMediaContent(PreparedMedia media) : AIContent
{
    /// <summary>
    ///     Подготовленное вложение: data-url и то, чем он будет для модели.
    /// </summary>
    public PreparedMedia Media { get; } = media;
}
