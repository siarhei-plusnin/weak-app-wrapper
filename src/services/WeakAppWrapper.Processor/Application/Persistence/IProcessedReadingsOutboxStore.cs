using WeakAppWrapper.Processor.Domain;

namespace WeakAppWrapper.Processor.Application.Persistence;

public interface IProcessedReadingsOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken);

    Task MarkPublishedAsync(Guid outboxMessageId, DateTimeOffset publishedAt, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid outboxMessageId, string errorMessage, CancellationToken cancellationToken);
}
