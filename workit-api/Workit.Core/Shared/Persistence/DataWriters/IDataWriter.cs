namespace Workit.Core.Shared.Persistence.DataWriters;

public interface IDataWriter
{
    IDataWriter Add<TEntity>(TEntity entity)
        where TEntity : class;

    IDataWriter Update<TEntity>(TEntity entity)
        where TEntity : class;

    IDataWriter Remove<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveAsync(CancellationToken cancellationToken = default);
}
