using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeakAppWrapper.Processor.Domain;

namespace WeakAppWrapper.Processor.Infrastructure.Persistence.Configurations;

public sealed class ReadingConfiguration : IEntityTypeConfiguration<Reading>
{
    public const string SourceMessageItemUniqueIndex = "uq_readings_source_message_item";

    public void Configure(EntityTypeBuilder<Reading> builder)
    {
        builder.ToTable("readings");
        builder.HasKey(reading => reading.Id).HasName("pk_readings");

        builder.Property(reading => reading.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(reading => reading.Source).HasColumnName("source").HasMaxLength(100).IsRequired();
        builder
            .Property(reading => reading.SourceMessageId)
            .HasColumnName("source_message_id")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(reading => reading.ItemIndex).HasColumnName("item_index").IsRequired();
        builder.Property(reading => reading.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
        builder.Property(reading => reading.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(reading => reading.RawRecord).HasColumnName("raw_record").HasColumnType("jsonb").IsRequired();
        builder.Property(reading => reading.CapturedAt).HasColumnName("captured_at").IsRequired();
        builder
            .Property(reading => reading.ProcessorReceivedAt)
            .HasColumnName("processor_received_at")
            .IsRequired();
        builder
            .Property(reading => reading.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder
            .HasIndex(reading => new { reading.Source, reading.SourceMessageId, reading.ItemIndex })
            .IsUnique()
            .HasDatabaseName(SourceMessageItemUniqueIndex);
        builder.HasIndex(reading => reading.CapturedAt).HasDatabaseName("ix_readings_captured_at");
        builder
            .HasIndex(reading => new { reading.Name, reading.CapturedAt })
            .HasDatabaseName("ix_readings_name_captured_at");
        builder
            .HasIndex(reading => new { reading.Type, reading.CapturedAt })
            .HasDatabaseName("ix_readings_type_captured_at");
    }
}
