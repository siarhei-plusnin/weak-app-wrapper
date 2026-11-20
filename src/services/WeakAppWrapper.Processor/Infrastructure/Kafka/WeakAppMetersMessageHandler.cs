using System.Text.Json;
using KafkaFlow;
using WeakAppWrapper.Contracts.Messages;
using WeakAppWrapper.Processor.Application;

namespace WeakAppWrapper.Processor.Infrastructure.Kafka;

public sealed class WeakAppMetersMessageHandler(WeakAppMetersProcessingService processingService)
    : IMessageHandler<WeakAppMetersPolledMessage>
{
    public Task Handle(IMessageContext context, WeakAppMetersPolledMessage message)
    {
        if (message is null)
        {
            throw new JsonException("Raw Kafka message must contain a WeakApp poll envelope");
        }

        return processingService.ProcessAsync(message, context.ConsumerContext.WorkerStopped);
    }
}
