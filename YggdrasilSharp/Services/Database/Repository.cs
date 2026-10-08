using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;

namespace Tavstal.YggdrasilSharp.Services.Database;

/// <inheritdoc />
[SuppressMessage("Trimming", "IL2091:Target generic argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in target method or type. The generic parameter of the source method or type does not have matching annotations.")]
public class Repository<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors |
    DynamicallyAccessedMemberTypes.PublicProperties |
    DynamicallyAccessedMemberTypes.NonPublicProperties)] T> : IRepository<T> where T : class
{
    /// <summary>
    /// The application database context used by the repository to access the database.
    /// </summary>
    protected readonly CustomDbContext _db;

    /// <summary>
    /// The EF Core <see cref="DbSet{T}"/> corresponding to the entity type <typeparamref name="T"/>.
    /// </summary>
    protected readonly DbSet<T> _set;

    /// <summary>
    /// Constructs a new repository instance bound to the provided <see cref="CustomDbContext"/>.
    /// After construction, <see cref="_set"/> is initialized to the context's set for <typeparamref name="T"/>.
    /// </summary>
    /// <param name="db">The database context to use. Typically provided by dependency injection.</param>
    public Repository(CustomDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    /// <inheritdoc />
    public async Task<T> AddAsync(T value, bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        var result = _set.Add(value);
        if (shouldSave)
            await _db.SaveChangesAsync(cancellationToken);
        return result.Entity;
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<T> values, bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        _set.AddRange(values);
        if (shouldSave)
            await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T> UpdateAsync(T value, bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        var result = _set.Update(value);
        if (shouldSave)
            await _db.SaveChangesAsync(cancellationToken);
        return result.Entity;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(T value, bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        _set.Remove(value);
        if (shouldSave)
            await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveRangeAsync(IEnumerable<T> values, bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        _set.RemoveRange(values);
        if (shouldSave)
            await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
    {
        var query = _set.AsQueryable();
        foreach (var include in includes)
            query = query.Include(include);
        if (predicate != null)
            query = query.Where(predicate);
        return await query.ToListAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> FindAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken = default)
    {
        if (predicate != null)
            return await _set.FirstOrDefaultAsync(predicate, cancellationToken: cancellationToken);
        return await _set.FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _set.AnyAsync(predicate, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> FindByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return await _set.FindAsync([id], cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) => await _db.SaveChangesAsync(cancellationToken);
}
