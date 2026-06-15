using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeakAppWrapper.Processor.Domain;

namespace WeakAppWrapper.Processor.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(outboxMessage => outboxMessage.Id).HasName("pk_outbox_messages");

        builder.Property(outboxMessage => outboxMessage.Id).HasColumnName("id").ValueGeneratedNever();
        builder
            .Property(outboxMessage => outboxMessage.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();
        builder
            .Property(outboxMessage => outboxMessage.MessageKey)
            .HasColumnName("message_key")
            .HasMaxLength(200)
            .IsRequired();
        builder
            .Property(outboxMessage => outboxMessage.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(200)
            .IsRequired();
        builder
            .Property(outboxMessage => outboxMessage.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(outboxMessage => outboxMessage.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(outboxMessage => outboxMessage.PublishedAt).HasColumnName("published_at");
        builder.Property(outboxMessage => outboxMessage.LastError).HasColumnName("last_error");

        builder
            .HasIndex(outboxMessage => new
            {
                outboxMessage.EventType,
                outboxMessage.PublishedAt,
                outboxMessage.OccurredAt,
            })
            .HasDatabaseName("ix_outbox_messages_pending");
        builder
            .HasIndex(outboxMessage => outboxMessage.CorrelationId)
            .HasDatabaseName("ix_outbox_messages_correlation_id");
    }
}
