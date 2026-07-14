using Workit.Core.Shared.Persistence;

namespace Workit.Core.Shared.Persistence.DataWriters;

public sealed class EfDataWriter(AppDbContext dbContext) : IDataWriter
{
    public IDataWriter Add<TEntity>(TEntity entity)
        where TEntity : class
    {
        dbContext.Set<TEntity>().Add(entity);
        return this;
    }

    public IDataWriter Update<TEntity>(TEntity entity)
        where TEntity : class
    {
        dbContext.Set<TEntity>().Update(entity);
        return this;
    }

    public IDataWriter Remove<TEntity>(TEntity entity)
        where TEntity : class
    {
        dbContext.Set<TEntity>().Remove(entity);
        return this;
    }

    public Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
