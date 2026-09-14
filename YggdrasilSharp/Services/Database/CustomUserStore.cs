using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;

// ReSharper disable UnusedAutoPropertyAccessor.Global - The properties in this class are intended to be accessed by higher-level services and controllers.

namespace Tavstal.YggdrasilSharp.Services.Database;

/// <summary>
/// Lightweight composition root that exposes repository instances for all user-related persistence concerns.
/// This class groups the repositories used by higher-level user management services (for example a custom user manager,
/// authentication services, or controllers) so they can be injected and used together.
/// </summary>
public class CustomUserStore
{
    /// <summary>
    /// Repository for the primary user entity (<see cref="CustomUser"/>).
    /// Use this to create, read, update and delete user records.
    /// </summary>
    private IRepository<CustomUser> Users { get; }
    
    /// <summary>
    /// Repository for user claims (<see cref="CustomUserClaim"/>).
    /// Stores claim entries associated with users (claim type/value pairs).
    /// </summary>
    public IRepository<CustomUserClaim> UserClaims { get; }
    
    /// <summary>
    /// Repository for user-role relationships (<see cref="CustomUserRole"/>).
    /// Use to manage which roles are assigned to which users.
    /// </summary>
    public IRepository<CustomUserRole> UserRoles { get; }
    
    /// <summary>
    /// Repository for roles (<see cref="CustomRole"/>).
    /// Contains available roles and their metadata.
    /// </summary>
    public IRepository<CustomRole> Roles { get; }
    
    /// <summary>
    /// Repository for persistent user tokens (<see cref="CustomUserToken"/>).
    /// Commonly used to store refresh tokens, confirmation tokens, or other long-lived tokens.
    /// </summary>
    public IRepository<CustomUserToken> UserTokens { get; }
    
    /// <summary>
    /// Repository for external/user login providers (<see cref="CustomUserLogin"/>).
    /// Stores login provider information (e.g. external OAuth provider keys) linked to users.
    /// </summary>
    public IRepository<CustomUserLogin> UserLogins { get; }
    
    /// <summary>
    /// Repository for role-claim records using the ASP.NET Core identity type <see cref="IdentityRoleClaim{TKey}"/>.
    /// Provides access to claims assigned to roles.
    /// </summary>
    public IRepository<IdentityRoleClaim<string>> RoleClaims { get; }

    /// <summary>
    /// Repository for user backup codes or recovery codes (<see cref="UserBackupCode"/>).
    /// Use when implementing account recovery / two-factor backup code storage.
    /// </summary>
    public IRepository<UserBackupCode> UserBackupCodes { get; }
    
    /// <summary>
    /// Repository for user billing information (<see cref="UserBillingInformation"/>).
    /// Stores payment-related or billing metadata for a user account.
    /// </summary>
    public IRepository<UserBillingInformation> UserBillingInformations { get; }
    
    /// <summary>
    /// Repository for user play sessions (<see cref="UserPlaySession"/>).
    /// Tracks session/activity related information for users (for example for analytics or session history).
    /// </summary>
    public IRepository<UserPlaySession> UserPlaySessions { get; }
    
    /// <summary>
    /// Repository for user capes (<see cref="UserCape"/>).
    /// Stores cosmetic cape items/customizations associated with users (for example player cosmetic decorations or skins).
    /// </summary>
    public IRepository<UserCape> UserCapes { get; }

    /// <summary>
    /// Constructs a new <see cref="CustomUserStore"/> instance with the specified repositories.
    /// </summary>
    /// <param name="users">Repository for <see cref="CustomUser"/> entities.</param>
    /// <param name="userClaims">Repository for <see cref="CustomUserClaim"/> entities.</param>
    /// <param name="userRoles">Repository for <see cref="CustomUserRole"/> entities.</param>
    /// <param name="roles">Repository for <see cref="CustomRole"/> entities.</param>
    /// <param name="userTokens">Repository for <see cref="CustomUserToken"/> entities.</param>
    /// <param name="userLogins">Repository for <see cref="CustomUserLogin"/> entities.</param>
    /// <param name="roleClaims">Repository for IdentityRoleClaim entities.</param>
    /// <param name="userBackupCodes">Repository for <see cref="UserBackupCode"/> entities.</param>
    /// <param name="userBillingInformations">Repository for <see cref="UserBillingInformation"/> entities.</param>
    /// <param name="userPlaySessions">Repository for <see cref="UserPlaySession"/> entities.</param>
    /// <param name="userCapes">Repository for <see cref="UserCape"/> entities.</param>
    /// <remarks>
    /// All repositories are intended to be provided by the dependency injection container. The store itself is a simple
    /// holder for these dependencies and does not perform additional validation. Consumers should handle null checks if required
    /// or ensure DI provides valid instances.
    /// </remarks>
    public CustomUserStore(
        IRepository<CustomUser> users,
        IRepository<CustomUserClaim> userClaims,
        IRepository<CustomUserRole> userRoles,
        IRepository<CustomRole> roles,
        IRepository<CustomUserToken> userTokens,
        IRepository<CustomUserLogin> userLogins,
        IRepository<IdentityRoleClaim<string>> roleClaims,
        IRepository<UserBackupCode> userBackupCodes,
        IRepository<UserBillingInformation> userBillingInformations,
        IRepository<UserPlaySession> userPlaySessions,
        IRepository<UserCape> userCapes)
    {
        Users = users;
        UserClaims = userClaims;
        UserRoles = userRoles;
        Roles = roles;
        UserTokens = userTokens;
        UserLogins = userLogins;
        RoleClaims = roleClaims;
        UserBackupCodes = userBackupCodes;
        UserBillingInformations = userBillingInformations;
        UserPlaySessions = userPlaySessions;
        UserCapes = userCapes;
    }
    
    /// <summary>
    /// Adds a new user to the store.
    /// Automatically generates a new security stamp and concurrency stamp for the user to ensure secure identity and optimistic concurrency control.
    /// </summary>
    /// <param name="value">The <see cref="CustomUser"/> entity to add.</param>
    /// <param name="shouldSave">If true, immediately persists changes to the database.</param>
    /// <param name="cancellationToken">Optional cancellation token for task cancellation.</param>
    /// <returns>The added <see cref="CustomUser"/> with generated security and concurrency stamps.</returns>

    public async Task<CustomUser> AddUserAsync(CustomUser value, bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        value.SecurityStamp = Guid.NewGuid().ToString();
        value.ConcurrencyStamp = Guid.NewGuid().ToString();
        return await Users.AddAsync(value, shouldSave, cancellationToken);
    }
    
    /// <summary>
    /// Updates an existing user in the store.
    /// Automatically regenerates the concurrency stamp to track modifications and prevent concurrent update conflicts.
    /// </summary>
    /// <param name="value">The <see cref="CustomUser"/> entity to update with new values.</param>
    /// <param name="shouldSave">If true, immediately persists changes to the database.</param>
    /// <param name="cancellationToken">Optional cancellation token for task cancellation.</param>
    public async Task<CustomUser> UpdateUserAsync(CustomUser value, bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        value.ConcurrencyStamp = Guid.NewGuid().ToString();
        return await Users.UpdateAsync(value, shouldSave, cancellationToken);
    }
    
    /// <summary>
    /// Removes (deletes) a user from the store.
    /// </summary>
    /// <param name="value">The <see cref="CustomUser"/> entity to remove.</param>
    /// <param name="shouldSave">If true, immediately persists the deletion to the database.</param>
    /// <param name="cancellationToken">Optional cancellation token for task cancellation.</param>
    /// <returns>A completed task representing the asynchronous removal operation.</returns>
    public async Task RemoveUserAsync(CustomUser value, bool shouldSave = false, CancellationToken cancellationToken = default) => await Users.RemoveAsync(value, shouldSave, cancellationToken);
    
    /// <summary>
    /// Queries users from the store using a LINQ expression predicate.
    /// Returns all users matching the provided filter condition.
    /// </summary>
    /// <param name="predicate">Optional LINQ expression to filter users. If null, returns all users.</param>
    /// <param name="cancellationToken">Optional cancellation token for task cancellation.</param>
    /// <returns>An enumerable collection of <see cref="CustomUser"/> entities matching the predicate.</returns>
    public async Task<IEnumerable<CustomUser>> QueryUserAsync(Expression<Func<CustomUser, bool>>? predicate, CancellationToken cancellationToken = default) => await Users.QueryAsync(predicate, cancellationToken);
    
    /// <summary>
    /// Finds a single user matching the provided predicate expression.
    /// Returns the first match or null if no user matches the condition.
    /// </summary>
    /// <param name="predicate">Optional LINQ expression to filter for a specific user. If null, returns the first user.</param>
    /// <param name="cancellationToken">Optional cancellation token for task cancellation.</param>
    /// <returns>The first <see cref="CustomUser"/> matching the predicate, or null if no match is found.</returns>
    public async Task<CustomUser?> FindUserAsync(Expression<Func<CustomUser, bool>>? predicate, CancellationToken cancellationToken = default) => await Users.FindAsync(predicate, cancellationToken);
    
    /// <summary>
    /// Determines whether any user exists in the store matching the provided predicate.
    /// </summary>
    /// <param name="predicate">LINQ expression to filter users.</param>
    /// <param name="cancellationToken">Optional cancellation token for task cancellation.</param>
    /// <returns>True if at least one user matches the predicate; false otherwise.</returns>
    public async Task<bool> ExistsUserAsync(Expression<Func<CustomUser, bool>> predicate, CancellationToken cancellationToken = default) => await Users.ExistsAsync(predicate, cancellationToken);

    /// <summary>
    /// Finds a user by their primary key (ID).
    /// </summary>
    /// <param name="id">The unique identifier of the user to retrieve. Typically a GUID or integer string.</param>
    /// <param name="cancellationToken">Optional cancellation token for task cancellation.</param>
    /// <returns>The <see cref="CustomUser"/> with the specified ID, or null if not found.</returns>
    public async Task<CustomUser?> FindUserByIdAsync(object id, CancellationToken cancellationToken = default) => await Users.FindByIdAsync(id, cancellationToken);
}