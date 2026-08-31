using System.Linq.Expressions;
using Workit.Core.Shared.Entities;

namespace Workit.Core.Shared.Persistence.GenericQueries;

public interface IGenericQuery
{
    public Task<T?> GetByIdAsync<T>(params object[] id)
        where T : class;

    public Task<IList<T>> GetByIdsAsync<T>(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
        where T : EntityWithGuid;

    public Task<IList<T>> GetByIdsAsync<T>(
        IReadOnlyList<string> ids,
        CancellationToken cancellationToken = default)
        where T : Entity<string>;

    public Task<T> GetByIdOrThrowAsync<T>(params object[] id)
        where T : class;

    public Task<bool> ExistsByIdAsync<T>(params object[] id)
        where T : class;

    public Task<bool> ExistsByPropertyAsync<T>(
        string propertyName,
        object propertyValue,
        CancellationToken cancellationToken = default)
        where T : class;

    public Task<bool> ExistsByPropertiesAsync<T>(
        IReadOnlyList<PropertyValue> properties,
        CancellationToken cancellationToken = default)
        where T : class;

    public Task<IList<T>> GetAllAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    public Task<long> CountAll<T>(CancellationToken cancellationToken = default)
        where T : class;

    public Task<T?> GetOneEntityByPropertyAsync<T>(
        string propertyName,
        object propertyValue,
        CancellationToken cancellationToken = default)
        where T : class;

    public Task<T?> GetOneEntityByPropertiesAsync<T>(
        IReadOnlyList<PropertyValue> properties,
        CancellationToken cancellationToken = default)
        where T : class;

    public Task<T> GetOneEntityByPropertyOrThrowAsync<T>(
        string propertyName,
        object propertyValue,
        CancellationToken cancellationToken = default)
        where T : class;

    public Task<T> GetOneEntityByPropertyOrThrowAsync<T>(
        IReadOnlyList<PropertyValue> properties,
        CancellationToken cancellationToken = default)
        where T : class;

    public Task<IList<T>> GetEntitiesByPropertyAsync<T>(
        string propertyName,
        object propertyValue,
        CancellationToken cancellationToken = default)
        where T : class;

    public Task<IList<T>> GetEntitiesByPropertyAsync<T>(
        IReadOnlyList<PropertyValue> properties,
        CancellationToken cancellationToken = default)
        where T : class;

    public Task<IList<T>> GetEntitiesAsync<T>(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : class;
}

public sealed record PropertyValue(string Name, object Value);
