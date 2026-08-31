using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Workit.Core.Shared.Persistence;

public abstract class WorkitDbContextBase<T>(
    DbContextOptions<T> options,
    ILoggerFactory? loggerFactory = null)
    : DbContext(options)
    where T : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        var internalServiceProvider = optionsBuilder.Options
            .FindExtension<CoreOptionsExtension>()
            ?.InternalServiceProvider;

        if (loggerFactory is not null && internalServiceProvider is null)
        {
            optionsBuilder.UseLoggerFactory(loggerFactory);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(WorkitDbContextBase<>).Assembly,
            type => type.Namespace is "Workit.Core.Shared.Persistence.Configurations");

        modelBuilder.MapDbFunctions();

        base.OnModelCreating(modelBuilder);
    }
}
