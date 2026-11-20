using Microsoft.EntityFrameworkCore;
using WeakAppWrapper.Processor.Application.Persistence;
using WeakAppWrapper.Processor.Domain;

namespace WeakAppWrapper.Processor.Infrastructure.Persistence;

public sealed class EfProcessedReadingsOutboxStore(ProcessorDbContext dbContext) : IProcessedReadingsOutboxStore
{
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken
    ) =>
        await dbContext
            .OutboxMessages.Where(message =>
                message.EventType == OutboxEventTypes.ReadingsProcessed && message.PublishedAt == null
            )
            .OrderBy(message => message.OccurredAt)
            .ThenBy(message => message.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task MarkPublishedAsync(
        Guid outboxMessageId,
        DateTimeOffset publishedAt,
        CancellationToken cancellationToken
    )
    {
        OutboxMessage message = await FindMessageAsync(outboxMessageId, cancellationToken);
        message.PublishedAt = publishedAt;
        message.LastError = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid outboxMessageId, string errorMessage, CancellationToken cancellationToken)
    {
        OutboxMessage message = await FindMessageAsync(outboxMessageId, cancellationToken);
        message.LastError = errorMessage.Length > 2_000 ? errorMessage[..2_000] : errorMessage;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<OutboxMessage> FindMessageAsync(Guid outboxMessageId, CancellationToken cancellationToken) =>
        await dbContext.OutboxMessages.SingleAsync(message => message.Id == outboxMessageId, cancellationToken);
}
