using System.Threading;
using System.Threading.Tasks;
using TgLlmBot.Models;

namespace TgLlmBot.Services.Llm.Descriptions;

/// <summary>
///     Описывает вложение основной моделью - той же, что отвечает на сообщения чата, - и превращает
///     его в компактное текстовое описание для истории.
/// </summary>
/// <remarks>
///     Описание нужно затем, чтобы вложение осталось в памяти бота после того, как уйти из контекста
///     само: в историю чата оно ложится текстом, а не медиа-файлом. Готовится в фоне, ответа не ждёт.
/// </remarks>
public interface IMediaDescriber
{
    /// <summary>
    ///     Описывает переданное вложение.
    /// </summary>
    /// <param name="request">Вложение и всё, что о нём известно.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Компактное описание вложения либо признак неудачи.</returns>
    Task<Result<string>> DescribeAsync(MediaDescriptionRequest request, CancellationToken cancellationToken);
}
