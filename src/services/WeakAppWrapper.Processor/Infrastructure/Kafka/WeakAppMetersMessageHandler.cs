using KafkaFlow;
using WeakAppWrapper.Contracts.Messages;
using WeakAppWrapper.Processor.Application;

namespace WeakAppWrapper.Processor.Infrastructure.Kafka;

public sealed class WeakAppMetersMessageHandler(WeakAppMetersProcessingService processingService)
    : IMessageHandler<WeakAppMetersPolledMessage>
{
    public Task Handle(IMessageContext context, WeakAppMetersPolledMessage message) =>
        processingService.ProcessAsync(message, context.ConsumerContext.WorkerStopped);
}
