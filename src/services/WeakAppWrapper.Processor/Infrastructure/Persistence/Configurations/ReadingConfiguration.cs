using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeakAppWrapper.Processor.Domain;

namespace WeakAppWrapper.Processor.Infrastructure.Persistence.Configurations;

public sealed class ReadingConfiguration : IEntityTypeConfiguration<Reading>
{
    public void Configure(EntityTypeBuilder<Reading> builder)
    {
        builder.ToTable("readings");
        builder.HasKey(reading => reading.Id).HasName("pk_readings");

        builder.Property(reading => reading.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(reading => reading.Source).HasColumnName("source").HasMaxLength(100).IsRequired();
        builder.Property(reading => reading.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
        builder.Property(reading => reading.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(reading => reading.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(reading => reading.ObservedAt).HasColumnName("observed_at").IsRequired();
        builder.Property(reading => reading.ReceivedAt).HasColumnName("received_at").IsRequired();
        builder
            .Property(reading => reading.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(reading => reading.ObservedAt).HasDatabaseName("ix_readings_observed_at");
        builder.HasIndex(reading => reading.Name).HasDatabaseName("ix_readings_name");
        builder.HasIndex(reading => reading.Type).HasDatabaseName("ix_readings_type");
        builder
            .HasIndex(reading => new { reading.Name, reading.ObservedAt })
            .HasDatabaseName("ix_readings_name_observed_at");
        builder
            .HasIndex(reading => new { reading.Type, reading.ObservedAt })
            .HasDatabaseName("ix_readings_type_observed_at");
    }
}
