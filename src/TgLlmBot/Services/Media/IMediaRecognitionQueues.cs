using System.Collections.Generic;
using System.Threading.Channels;

namespace TgLlmBot.Services.Media;

/// <summary>
///     Пул per-chat очередей описания вложений - по одной очереди на каждый разрешённый чат.
///     Внутри чата задания обрабатываются строго последовательно, разные чаты - параллельно.
/// </summary>
/// <remarks>
///     Очередь нужна только ради истории чата: описания должны появляться и у вложений сообщений,
///     на которые бот не отвечал, иначе спросить про присланный полчаса назад мем будет не о чем.
/// </remarks>
public interface IMediaRecognitionQueues
{
    /// <summary>
    ///     Читатели очередей, сгруппированные по идентификатору чата.
    /// </summary>
    IReadOnlyDictionary<long, ChannelReader<MediaRecognitionJob>> Readers { get; }

    /// <summary>
    ///     Помещает задание в очередь чата, к которому оно относится.
    /// </summary>
    /// <returns>
    ///     <see langword="false" />, если для чата нет очереди, очередь завершена или задание
    ///     отброшено переполнившейся очередью. Недоделанное подберёт подчистка.
    /// </returns>
    bool TryEnqueue(long chatId, MediaRecognitionJob job);

    /// <summary>
    ///     Завершает все очереди - новые задания больше не принимаются, уже поставленные будут дочитаны.
    /// </summary>
    void Complete();
}
