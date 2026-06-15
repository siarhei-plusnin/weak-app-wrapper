using System.Text;
using System.Text.Json;
using KafkaFlow;
using WeakAppWrapper.Contracts.Messages;
using WeakAppWrapper.Processor.Application;

namespace WeakAppWrapper.Processor.Infrastructure.Kafka;

public sealed class RawMessageDeadLetterMiddleware(
    IProcessingEventPublisher eventPublisher,
    ILogger<RawMessageDeadLetterMiddleware> logger
) : IMessageMiddleware
{
    private const string Source = "kafka";

    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            string sourceMessageId =
                ReadMessagePart(context.Message.Key)
                ?? $"{context.ConsumerContext.Topic}-{context.ConsumerContext.Partition}-{context.ConsumerContext.Offset}";

            JsonElement rawMessage = JsonSerializer.SerializeToElement(
                new
                {
                    context.ConsumerContext.Topic,
                    context.ConsumerContext.Partition,
                    context.ConsumerContext.Offset,
                    Key = ReadMessagePart(context.Message.Key),
                    Value = ReadMessagePart(context.Message.Value),
                }
            );

            await eventPublisher.PublishRawDeadLetterAsync(
                new RawMessageDeadLetterMessage(
                    Guid.NewGuid().ToString("D"),
                    sourceMessageId,
                    Source,
                    DateTimeOffset.UtcNow,
                    exception.GetType().Name,
                    exception.Message,
                    rawMessage
                ),
                context.ConsumerContext.WorkerStopped
            );
            logger.PublishedMalformedRawMessageToDeadLetter(exception, sourceMessageId);
        }
    }

    private static string? ReadMessagePart(object? value) =>
        value switch
        {
            null => null,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            JsonElement json => json.GetRawText(),
            _ => JsonSerializer.Serialize(value),
        };
}

internal static partial class RawMessageDeadLetterMiddlewareLogs
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Published malformed raw Kafka message {SourceMessageId} to the raw DLQ"
    )]
    internal static partial void PublishedMalformedRawMessageToDeadLetter(
        this ILogger logger,
        Exception exception,
        string sourceMessageId
    );
}
