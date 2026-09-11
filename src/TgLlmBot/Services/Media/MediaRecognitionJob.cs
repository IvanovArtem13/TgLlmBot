using System;
using TgLlmBot.DataAccess.Models;

namespace TgLlmBot.Services.Media;

/// <summary>
///     Задание на описание вложений одного сообщения чата - для истории.
/// </summary>
/// <remarks>
///     Ответ бота вложений не ждёт: свои вложения он видит в запросе сам. Задание готовит
///     то, что останется в памяти бота, когда сообщение уйдёт в прошлое.
/// </remarks>
public sealed class MediaRecognitionJob
{
    public MediaRecognitionJob(DbChatMessage storedMessage)
    {
        ArgumentNullException.ThrowIfNull(storedMessage);
        StoredMessage = storedMessage;
    }

    public long ChatId => StoredMessage.ChatId;

    public int MessageId => StoredMessage.MessageId;

    /// <summary>
    ///     Сохранённая строка истории с коллекцией вложений - источник вложений для описания.
    /// </summary>
    public DbChatMessage StoredMessage { get; }
}
