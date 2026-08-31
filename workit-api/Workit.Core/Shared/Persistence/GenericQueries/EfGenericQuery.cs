using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Entities;
using Workit.Core.Shared.Exceptions;

namespace Workit.Core.Shared.Persistence.GenericQueries;

public sealed class EfGenericQuery(AppDbContext db) : IGenericQuery
{
    public async Task<T?> GetByIdAsync<T>(params object[] id)
        where T : class
    {
        return await db.Set<T>().FindAsync(id);
    }

    public async Task<IList<T>> GetByIdsAsync<T>(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
        where T : EntityWithGuid
    {
        return await db.Set<T>()
            .Where(entity => ids.Contains(entity.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<T>> GetByIdsAsync<T>(
        IReadOnlyList<string> ids,
        CancellationToken cancellationToken = default)
        where T : Entity<string>
    {
        return await db.Set<T>()
            .Where(entity => ids.Contains(entity.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<T> GetByIdOrThrowAsync<T>(params object[] id)
        where T : class
    {
        var result = await GetByIdAsync<T>(id);
        return result
               ?? throw new NotFoundException(
                   $"Entity {typeof(T).FullName} with id {string.Join(",", id)} not found in database.");
    }

    public async Task<bool> ExistsByIdAsync<T>(params object[] id)
        where T : class
    {
        var item = await GetByIdAsync<T>(id);
        return item is not null;
    }

    public async Task<bool> ExistsByPropertyAsync<T>(
        string propertyName,
        object propertyValue,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return await db.Set<T>()
            .AnyAsync(
                entity => Equals(EF.Property<object>(entity, propertyName), propertyValue),
                cancellationToken);
    }

    public async Task<bool> ExistsByPropertiesAsync<T>(
        IReadOnlyList<PropertyValue> properties,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var query = db.Set<T>().AsQueryable();

        foreach (var property in properties)
        {
            query = query.Where(entity => Equals(EF.Property<object>(entity, property.Name), property.Value));
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IList<T>> GetAllAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return await db.Set<T>().ToListAsync(cancellationToken);
    }

    public async Task<long> CountAll<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return await db.Set<T>().LongCountAsync(cancellationToken);
    }

    public async Task<T?> GetOneEntityByPropertyAsync<T>(
        string propertyName,
        object propertyValue,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return await db.Set<T>()
            .FirstOrDefaultAsync(
                entity => Equals(EF.Property<object>(entity, propertyName), propertyValue),
                cancellationToken);
    }

    public async Task<T?> GetOneEntityByPropertiesAsync<T>(
        IReadOnlyList<PropertyValue> properties,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var query = db.Set<T>().AsQueryable();

        foreach (var property in properties)
        {
            query = query.Where(entity => Equals(EF.Property<object>(entity, property.Name), property.Value));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<T> GetOneEntityByPropertyOrThrowAsync<T>(
        string propertyName,
        object propertyValue,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var result = await GetOneEntityByPropertyAsync<T>(propertyName, propertyValue, cancellationToken);

        return result
               ?? throw new NotFoundException(
                   $"Entity {typeof(T).FullName} with property {propertyName} not found in database.");
    }

    public async Task<T> GetOneEntityByPropertyOrThrowAsync<T>(
        IReadOnlyList<PropertyValue> properties,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var result = await GetOneEntityByPropertiesAsync<T>(properties, cancellationToken);

        return result
               ?? throw new NotFoundException(
                   $"Entity {typeof(T).FullName} with properties {string.Join(",", properties)} not found in database.");
    }

    public async Task<IList<T>> GetEntitiesByPropertyAsync<T>(
        string propertyName,
        object propertyValue,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return await db.Set<T>()
            .Where(entity => Equals(EF.Property<object>(entity, propertyName), propertyValue))
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<T>> GetEntitiesByPropertyAsync<T>(
        IReadOnlyList<PropertyValue> properties,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var query = db.Set<T>().AsQueryable();

        foreach (var property in properties)
        {
            query = query.Where(entity => Equals(EF.Property<object>(entity, property.Name), property.Value));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IList<T>> GetEntitiesAsync<T>(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return await db.Set<T>()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }
}
