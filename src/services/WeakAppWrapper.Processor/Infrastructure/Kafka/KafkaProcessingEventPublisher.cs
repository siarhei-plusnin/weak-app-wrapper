using KafkaFlow.Producers;
using Microsoft.Extensions.Options;
using WeakAppWrapper.Contracts.Messages;
using WeakAppWrapper.Processor.Application;
using WeakAppWrapper.Processor.Configuration;

namespace WeakAppWrapper.Processor.Infrastructure.Kafka;

public sealed class KafkaProcessingEventPublisher(
    IProducerAccessor producerAccessor,
    IOptionsMonitor<KafkaConfiguration> kafkaOptions
) : IProcessingEventPublisher
{
    internal const string ProcessedProducerName = "processed-readings";
    internal const string RawDeadLetterProducerName = "raw-dead-letters";

    public Task PublishProcessedAsync(ProcessedReadingMessage message, CancellationToken cancellationToken) =>
        producerAccessor
            .GetProducer(ProcessedProducerName)
            .ProduceAsync(kafkaOptions.CurrentValue.ProcessedTopic, message.ReadingId.ToString("D"), message);

    public Task PublishRawDeadLetterAsync(RawMessageDeadLetterMessage message, CancellationToken cancellationToken) =>
        producerAccessor
            .GetProducer(RawDeadLetterProducerName)
            .ProduceAsync(kafkaOptions.CurrentValue.RawDeadLetterTopic, message.SourceMessageId, message);
}
