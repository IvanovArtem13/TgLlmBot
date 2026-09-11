namespace TgLlmBot.DataAccess.Models;

/// <summary>
///     Состояние обработки вложения.
/// </summary>
public enum DbMediaRecognitionStatus
{
    /// <summary>
    ///     Вложение поставлено в очередь, но ещё не распознано.
    /// </summary>
    Pending = 0,

    /// <summary>
    ///     Сжатое описание готово и лежит в <see cref="DbChatMessageMedia.ShortDescription" /> -
    ///     именно оно уходит в историю чата.
    /// </summary>
    Ready = 1,

    /// <summary>
    ///     Описать не удалось: не скачалось, не открылось или модель вернула ошибку.
    /// </summary>
    Failed = 2,

    /// <summary>
    ///     Показать модели нечего: у вложения нет ни картинки, ни статического превью
    ///     (например, анимированный стикер без thumbnail), либо капабилити модели такое
    ///     вложение не покрывают.
    /// </summary>
    Unsupported = 3
}
