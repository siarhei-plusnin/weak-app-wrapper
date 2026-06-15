using Microsoft.EntityFrameworkCore;
using WeakAppWrapper.Processor.Domain;

namespace WeakAppWrapper.Processor.Infrastructure.Persistence;

public sealed class ProcessorDbContext(DbContextOptions<ProcessorDbContext> options) : DbContext(options)
{
    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcessorDbContext).Assembly);
    }
}
