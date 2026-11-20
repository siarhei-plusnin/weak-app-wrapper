using WeakAppWrapper.Contracts.Messages;

namespace WeakAppWrapper.Processor.Application;

public interface IProcessingEventPublisher
{
    Task PublishProcessedAsync(ProcessedReadingMessage message, CancellationToken cancellationToken);

    Task PublishRawDeadLetterAsync(RawMessageDeadLetterMessage message, CancellationToken cancellationToken);
}
