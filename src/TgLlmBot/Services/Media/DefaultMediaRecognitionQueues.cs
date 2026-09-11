using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace TgLlmBot.Services.Media;

public partial class DefaultMediaRecognitionQueues : IMediaRecognitionQueues
{
    private readonly ILogger<DefaultMediaRecognitionQueues> _logger;
    private readonly FrozenDictionary<long, Channel<MediaRecognitionJob>> _queues;

    public DefaultMediaRecognitionQueues(
        DefaultMediaRecognitionQueuesOptions options,
        ILogger<DefaultMediaRecognitionQueues> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        var queues = new Dictionary<long, Channel<MediaRecognitionJob>>(options.ChatIds.Count);
        var readers = new Dictionary<long, ChannelReader<MediaRecognitionJob>>(options.ChatIds.Count);
        foreach (var chatId in options.ChatIds)
        {
            var queue = CreateChatQueue(chatId, options.CapacityPerChat, logger);
            queues.Add(chatId, queue);
            readers.Add(chatId, queue.Reader);
        }

        _queues = queues.ToFrozenDictionary();
        Readers = readers.ToFrozenDictionary();
    }

    public IReadOnlyDictionary<long, ChannelReader<MediaRecognitionJob>> Readers { get; }

    public bool TryEnqueue(long chatId, MediaRecognitionJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (!_queues.TryGetValue(chatId, out var queue))
        {
            return false;
        }

        if (queue.Writer.TryWrite(job))
        {
            Log.JobEnqueued(_logger, chatId, job.MessageId);
            return true;
        }

        // Очередь переполнена: задание уже отброшено каналом (с логированием в колбэке дропа),
        // в базе вложение останется Pending и его подберёт подчистка
        return false;
    }

    public void Complete()
    {
        foreach (var queue in _queues.Values)
        {
            queue.Writer.TryComplete();
        }
    }

    private static Channel<MediaRecognitionJob> CreateChatQueue(long chatId, int capacity, ILogger<DefaultMediaRecognitionQueues> logger)
    {
        // Bounded-канал с дропом: историю при переполнении отбрасываем, недоделанное
        // подберёт подчистка
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };
        return Channel.CreateBounded<MediaRecognitionJob>(
            options,
            dropped => Log.JobDropped(logger, chatId, dropped.MessageId, capacity));
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Debug, Message = "Enqueued attachments of message {MessageId} of chat {ChatId} for background description")]
        public static partial void JobEnqueued(ILogger logger, long chatId, int messageId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Media description queue of chat {ChatId} is full ({Capacity}), message {MessageId} dropped. It will be picked up by the sweep")]
        public static partial void JobDropped(ILogger logger, long chatId, int messageId, int capacity);
    }
}
