using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WeakAppWrapper.Contracts.Messages;
using WeakAppWrapper.Persistence.Domain;
using WeakAppWrapper.Persistence.Infrastructure.Persistence;
using WeakAppWrapper.Persistence.Infrastructure.Persistence.Configurations;
using WeakAppWrapper.Processor.Domain;

namespace WeakAppWrapper.Processor.Application;

public sealed class WeakAppMetersProcessingService(
    ApplicationDbContext dbContext,
    IProcessingEventPublisher eventPublisher,
    ILogger<WeakAppMetersProcessingService> logger
)
{
    private const string DefaultSource = "weakapp";

    public async Task ProcessAsync(WeakAppMetersPolledMessage message, CancellationToken cancellationToken)
    {
        IReadOnlyList<NormalizedReading> normalizedReadings;
        DateTimeOffset processorReceivedAt = DateTimeOffset.UtcNow;

        try
        {
            normalizedReadings = WeakAppReadingNormalizer.Normalize(message);
        }
        catch (WeakAppPayloadException exception)
        {
            await PublishRawMessageToDeadLetterAsync(message, exception, cancellationToken);

            return;
        }

        if (normalizedReadings.Count == 0)
        {
            logger.NoReadingsFound(message.MessageId);
            return;
        }

        string sourceMessageId = normalizedReadings[0].SourceMessageId;
        string source = normalizedReadings[0].Source;
        HashSet<int> existingItemIndexes = await dbContext
            .Readings.Where(reading => reading.Source == source && reading.SourceMessageId == sourceMessageId)
            .Select(reading => reading.ItemIndex)
            .ToHashSetAsync(cancellationToken);

        if (existingItemIndexes.Count > 0)
        {
            if (normalizedReadings.All(reading => existingItemIndexes.Contains(reading.ItemIndex)))
            {
                logger.SkippedDuplicateWeakAppMetersMessage(message.MessageId, normalizedReadings.Count);
                return;
            }

            await PublishRawMessageToDeadLetterAsync(
                message,
                new WeakAppPayloadException("WeakApp reused a message ID with a different item set"),
                cancellationToken
            );
            return;
        }

        var readings = new List<Reading>(normalizedReadings.Count);
        var outboxMessages = new List<OutboxMessage>(normalizedReadings.Count);
        DateTimeOffset processedAt = DateTimeOffset.UtcNow;

        foreach (NormalizedReading normalizedReading in normalizedReadings)
        {
            var reading = new Reading
            {
                Source = normalizedReading.Source,
                SourceMessageId = normalizedReading.SourceMessageId,
                ItemIndex = normalizedReading.ItemIndex,
                Type = normalizedReading.Type,
                Name = normalizedReading.Name,
                RawRecord = normalizedReading.RawRecord.GetRawText(),
                CapturedAt = normalizedReading.CapturedAt,
                ProcessorReceivedAt = processorReceivedAt,
                CreatedAt = processedAt,
            };

            var outboxMessage = new OutboxMessage
            {
                EventType = OutboxEventTypes.ReadingsProcessed,
                MessageKey = reading.Id.ToString("D"),
                CorrelationId = sourceMessageId,
                Payload = JsonSerializer.Serialize(
                    new ProcessedReadingMessage(
                        1,
                        Guid.NewGuid().ToString("D"),
                        sourceMessageId,
                        reading.Id,
                        reading.ItemIndex,
                        reading.Source,
                        reading.Type,
                        reading.Name,
                        reading.Name,
                        reading.CapturedAt,
                        reading.ProcessorReceivedAt,
                        processedAt,
                        normalizedReading.Payload
                    )
                ),
                OccurredAt = reading.CreatedAt,
            };

            readings.Add(reading);
            outboxMessages.Add(outboxMessage);
        }

        dbContext.Readings.AddRange(readings);
        dbContext.OutboxMessages.AddRange(outboxMessages);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateSourceMessageItem(exception))
        {
            dbContext.ChangeTracker.Clear();
            HashSet<int> persistedItemIndexes = await dbContext
                .Readings.Where(reading => reading.Source == source && reading.SourceMessageId == sourceMessageId)
                .Select(reading => reading.ItemIndex)
                .ToHashSetAsync(cancellationToken);

            if (normalizedReadings.All(reading => persistedItemIndexes.Contains(reading.ItemIndex)))
            {
                logger.SkippedDuplicateWeakAppMetersMessage(message.MessageId, normalizedReadings.Count);
                return;
            }

            await PublishRawMessageToDeadLetterAsync(
                message,
                new WeakAppPayloadException("WeakApp reused a message ID with a different item set"),
                cancellationToken
            );
            return;
        }

        logger.ProcessedWeakAppMetersMessage(message.MessageId, readings.Count, outboxMessages.Count);
    }

    private static bool IsDuplicateSourceMessageItem(DbUpdateException exception) =>
        exception.GetBaseException() is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: ReadingConfiguration.SourceMessageItemUniqueIndex,
        };

    private async Task PublishRawMessageToDeadLetterAsync(
        WeakAppMetersPolledMessage message,
        WeakAppPayloadException exception,
        CancellationToken cancellationToken
    )
    {
        DateTimeOffset failedAt = DateTimeOffset.UtcNow;
        string source = string.IsNullOrWhiteSpace(message.Source) ? DefaultSource : message.Source.Trim();
        string sourceMessageId = string.IsNullOrWhiteSpace(message.MessageId)
            ? Guid.NewGuid().ToString("D")
            : message.MessageId.Trim();
        var deadLetterMessage = new RawMessageDeadLetterMessage(
            Guid.NewGuid().ToString("D"),
            sourceMessageId,
            source,
            failedAt,
            exception.GetType().Name,
            exception.Message,
            CreateRawMessageElement(message)
        );

        await eventPublisher.PublishRawDeadLetterAsync(deadLetterMessage, cancellationToken);

        logger.PublishedRawMessageToDeadLetter(exception, sourceMessageId);
    }

    private static JsonElement CreateRawMessageElement(WeakAppMetersPolledMessage message)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("messageId", message.MessageId);
            writer.WriteString("source", message.Source);
            writer.WriteString("fetchedAt", message.FetchedAt);
            writer.WritePropertyName("payload");

            if (message.Payload.ValueKind == JsonValueKind.Undefined)
            {
                writer.WriteNullValue();
            }
            else
            {
                message.Payload.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        using JsonDocument rawMessage = JsonDocument.Parse(buffer.ToArray());
        return rawMessage.RootElement.Clone();
    }
}

internal static partial class WeakAppMetersProcessingServiceLogs
{
    [LoggerMessage(Level = LogLevel.Information, Message = "WeakApp meters message {MessageId} contained no readings")]
    internal static partial void NoReadingsFound(this ILogger logger, string messageId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Skipped duplicate WeakApp meters message {MessageId}: {ReadingCount} readings already persisted"
    )]
    internal static partial void SkippedDuplicateWeakAppMetersMessage(this ILogger logger, string messageId, int readingCount);

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
