using System.Text.Json;
using Microsoft.Extensions.Options;
using WeakAppWrapper.Contracts.Messages;
using WeakAppWrapper.Processor.Application;
using WeakAppWrapper.Processor.Application.Persistence;
using WeakAppWrapper.Processor.Configuration;
using WeakAppWrapper.Processor.Domain;

namespace WeakAppWrapper.Processor.Infrastructure.Outbox;

public sealed class ProcessedReadingsOutboxWorker(
    IProcessingEventPublisher eventPublisher,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<OutboxConfiguration> outboxOptions,
    ILogger<ProcessedReadingsOutboxWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            OutboxConfiguration options = outboxOptions.CurrentValue;
            await PublishAvailableMessagesAsync(options, stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task PublishAvailableMessagesAsync(OutboxConfiguration options, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IProcessedReadingsOutboxStore outboxStore =
            scope.ServiceProvider.GetRequiredService<IProcessedReadingsOutboxStore>();

        IReadOnlyList<OutboxMessage> messages = await outboxStore.GetPendingAsync(options.BatchSize, cancellationToken);

        foreach (OutboxMessage outboxMessage in messages)
        {
            try
            {
                ProcessedReadingMessage message =
                    JsonSerializer.Deserialize<ProcessedReadingMessage>(outboxMessage.Payload)
                    ?? throw new InvalidOperationException("Outbox payload deserialized to null");

                await eventPublisher.PublishProcessedAsync(message, cancellationToken);
                await outboxStore.MarkPublishedAsync(outboxMessage.Id, DateTimeOffset.UtcNow, cancellationToken);
                logger.PublishedProcessedReadingOutboxMessage(outboxMessage.Id, outboxMessage.MessageKey);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                await outboxStore.MarkFailedAsync(outboxMessage.Id, exception.Message, cancellationToken);
                logger.FailedProcessedReadingOutboxMessage(exception, outboxMessage.Id);
            }
        }
    }
}

internal static partial class ProcessedReadingsOutboxWorkerLogs
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Published processed reading outbox message {OutboxMessageId} with key {MessageKey}"
    )]
    internal static partial void PublishedProcessedReadingOutboxMessage(
        this ILogger logger,
        Guid outboxMessageId,
        string messageKey
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to publish processed reading outbox message {OutboxMessageId}"
    )]
    internal static partial void FailedProcessedReadingOutboxMessage(
        this ILogger logger,
        Exception exception,
        Guid outboxMessageId
    );
}
