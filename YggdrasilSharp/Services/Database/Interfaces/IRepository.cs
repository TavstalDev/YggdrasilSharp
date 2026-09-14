using System.Linq.Expressions;

namespace Tavstal.YggdrasilSharp.Services.Database.Interfaces;

/// <summary>
/// Generic repository contract for performing basic CRUD and query operations on entities of type <typeparamref name="T"/>.
/// Implementations are expected to provide asynchronous, persistence-aware operations against an underlying data store
/// (for example Entity Framework Core DbContext, Dapper, or any other persistence mechanism).
/// </summary>
/// <typeparam name="T">The entity type the repository handles. Must be a reference type (class).</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Adds a single entity to the repository.
    /// </summary>
    /// <param name="value">The entity instance to add.</param>
    /// <param name="shouldSave">
    /// If true, the implementation should persist the change immediately (for example call SaveChangesAsync).
    /// If false, the caller is responsible for persisting the change later.
    /// Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous add operation. The task result contains the added entity
    /// (some implementations may return the same instance, others may return an attached/tracked instance).
    /// </returns>
    Task<T> AddAsync(T value, bool shouldSave = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds multiple entities to the repository in a single operation.
    /// </summary>
    /// <param name="values">The collection of entities to add.</param>
    /// <param name="shouldSave">
    /// If true, persist changes immediately. If false, defer persistence for batching.
    /// Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    Task AddRangeAsync(IEnumerable<T> values, bool shouldSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="value">The entity with updated values. Implementations typically match this to an existing tracked entity.</param>
    /// <param name="shouldSave">
    /// If true, persist the update immediately; otherwise defer persistence.
    /// Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task<T> UpdateAsync(T value, bool shouldSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="value">The entity to remove.</param>
    /// <param name="shouldSave">
    /// If true, persist removal immediately; otherwise the caller must persist later.
    /// Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous remove operation.</returns>
    Task RemoveAsync(T value, bool shouldSave = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes multiple entities from the repository.
    /// </summary>
    /// <param name="values">The entities to remove.</param>
    /// <param name="shouldSave">
    /// If true, persist all removals immediately; otherwise defer persistence.
    /// Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous remove operation.</returns>
    Task RemoveRangeAsync(IEnumerable<T> values, bool shouldSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the repository for entities that satisfy the provided predicate expression.
    /// </summary>
    /// <param name="predicate">An expression used to filter entities. Implementations should translate this expression to the underlying query provider when possible.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <param name="includes">Optional expressions specifying related entities to include in the query results (for example, navigation properties in Entity Framework Core).</param>
    /// <returns>
    /// A task that represents the asynchronous query operation. The task result contains an enumerable of matching entities.
    /// </returns>
    Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Finds a single entity that matches the provided predicate.
    /// </summary>
    /// <param name="predicate">An expression used to locate the entity.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous find operation. The task result is the matching entity if found; otherwise <c>null</c>.
    /// </returns>
    Task<T?> FindAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines whether any entity exists in the repository that satisfies the specified predicate.
    /// </summary>
    /// <param name="predicate">A filter expression used to identify matching entities.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that resolves to <c>true</c> if at least one matching entity exists; otherwise <c>false</c>.
    /// </returns>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds an entity by its primary key identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity. Type is <see cref="object"/> to allow flexibility (GUID, int, string, etc.). Implementations should document the accepted id types and conversions.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous find-by-id operation. The task result is the found entity if present; otherwise <c>null</c>.
    /// </returns>
    Task<T?> FindByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists any pending changes tracked by the repository to the underlying data store asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
