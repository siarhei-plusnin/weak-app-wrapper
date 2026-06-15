using System.Text.Json;
using WeakAppWrapper.Contracts.Messages;
using WeakAppWrapper.Processor.Domain;
using WeakAppWrapper.Processor.Infrastructure.Persistence;

namespace WeakAppWrapper.Processor.Application;

public sealed class WeakAppMetersProcessingService(
    ProcessorDbContext dbContext,
    IProcessingEventPublisher eventPublisher,
    ILogger<WeakAppMetersProcessingService> logger
)
{
    private const string DefaultSource = "weakapp";

    public async Task ProcessAsync(WeakAppMetersPolledMessage message, CancellationToken cancellationToken)
    {
        IReadOnlyList<NormalizedReading> normalizedReadings;

        try
        {
            normalizedReadings = WeakAppReadingNormalizer.Normalize(message);
        }
        catch (WeakAppPayloadException exception)
        {
            DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
            string rawMessage = JsonSerializer.Serialize(message);
            string source = message.Source ?? DefaultSource;

            using JsonDocument rawMessageDocument = JsonDocument.Parse(rawMessage);
            var deadLetterMessage = new RawMessageDeadLetterMessage(
                Guid.NewGuid().ToString("D"),
                message.MessageId,
                source,
                occurredAt,
                exception.GetType().Name,
                exception.Message,
                rawMessageDocument.RootElement.Clone()
            );

            await eventPublisher.PublishRawDeadLetterAsync(deadLetterMessage, cancellationToken);

            logger.PublishedRawMessageToDeadLetter(exception, message.MessageId);

            return;
        }

        if (normalizedReadings.Count == 0)
        {
            logger.NoReadingsFound(message.MessageId);
            return;
        }

        var readings = new List<Reading>(normalizedReadings.Count);
        var outboxMessages = new List<OutboxMessage>(normalizedReadings.Count);

        foreach (NormalizedReading normalizedReading in normalizedReadings)
        {
            var reading = new Reading
            {
                Source = normalizedReading.Source,
                Type = normalizedReading.Type,
                Name = normalizedReading.Name,
                Payload = normalizedReading.Payload.GetRawText(),
                ObservedAt = normalizedReading.ObservedAt,
                ReceivedAt = normalizedReading.ReceivedAt,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var outboxMessage = new OutboxMessage
            {
                EventType = OutboxEventTypes.ReadingsProcessed,
                MessageKey = reading.Id.ToString("D"),
                CorrelationId = message.MessageId,
                Payload = JsonSerializer.Serialize(
                    new ProcessedReadingMessage(
                        Guid.NewGuid().ToString("D"),
                        message.MessageId,
                        reading.Id,
                        reading.Source,
                        reading.Type,
                        reading.Name,
                        reading.ObservedAt,
                        reading.ReceivedAt,
                        DateTimeOffset.UtcNow
                    )
                ),
                OccurredAt = reading.CreatedAt,
            };

            readings.Add(reading);
            outboxMessages.Add(outboxMessage);
        }

        dbContext.Readings.AddRange(readings);
        dbContext.OutboxMessages.AddRange(outboxMessages);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.ProcessedWeakAppMetersMessage(message.MessageId, readings.Count, outboxMessages.Count);
    }
}

internal static partial class WeakAppMetersProcessingServiceLogs
{
    [LoggerMessage(Level = LogLevel.Information, Message = "WeakApp meters message {MessageId} contained no readings")]
    internal static partial void NoReadingsFound(this ILogger logger, string messageId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Processed WeakApp meters message {MessageId}: {ReadingCount} readings inserted, {OutboxCount} outbox messages enqueued"
    )]
    internal static partial void ProcessedWeakAppMetersMessage(
        this ILogger logger,
        string messageId,
        int readingCount,
        int outboxCount
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Published unprocessed WeakApp meters message {MessageId} to the raw DLQ"
    )]
    internal static partial void PublishedRawMessageToDeadLetter(
        this ILogger logger,
        Exception exception,
        string messageId
    );
}
