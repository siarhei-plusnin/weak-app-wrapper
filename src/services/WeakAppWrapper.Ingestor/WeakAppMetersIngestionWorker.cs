using System.Text.Json;
using KafkaFlow.Producers;
using Microsoft.Extensions.Options;
using WeakAppWrapper.Contracts.Messages;
using WeakAppWrapper.Ingestor.Application;
using WeakAppWrapper.Ingestor.Configuration;

namespace WeakAppWrapper.Ingestor;

public sealed class WeakAppMetersIngestionWorker(
    IWeakAppMetersClient weakAppMetersClient,
    IProducerAccessor producerAccessor,
    IOptionsMonitor<IngestorConfiguration> ingestorOptions,
    IOptionsMonitor<KafkaConfiguration> kafkaOptions,
    ILogger<WeakAppMetersIngestionWorker> logger
) : BackgroundService
{
    internal const string KafkaProducerName = "weakapp-meters";
    private const string Source = "weakapp";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IngestorConfiguration options = ingestorOptions.CurrentValue;
        TimeSpan pollInterval = TimeSpan.FromSeconds(options.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await PollAndPublishAsync(stoppingToken);

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task PollAndPublishAsync(CancellationToken cancellationToken)
    {
        KafkaConfiguration options = kafkaOptions.CurrentValue;

        try
        {
            logger.PollingWeakAppMeters();

            string rawPayload = await weakAppMetersClient.QueryMetersAsync(cancellationToken);
            DateTimeOffset fetchedAt = DateTimeOffset.UtcNow;

            using JsonDocument payload = JsonDocument.Parse(rawPayload);
            string messageId = Guid.NewGuid().ToString("D");
            var message = new WeakAppMetersPolledMessage(messageId, Source, fetchedAt, payload.RootElement.Clone());

            await producerAccessor
                .GetProducer(KafkaProducerName)
                .ProduceAsync(options.Topic, message.MessageId, message);

            logger.PublishedWeakAppMeters(message.MessageId, options.Topic);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.PollingOrPublishingFailed(exception);
        }
    }
}

internal static partial class WeakAppMetersIngestionWorkerLogs
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Polling WeakApp meters")]
    internal static partial void PollingWeakAppMeters(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Published WeakApp meters message {MessageId} to Kafka topic {Topic}"
    )]
    internal static partial void PublishedWeakAppMeters(this ILogger logger, string messageId, string topic);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to poll WeakApp meters or publish them to Kafka")]
    internal static partial void PollingOrPublishingFailed(this ILogger logger, Exception exception);
}
