using Tavstal.YggdrasilSharp.Models.Claims;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Database.User.Claims;

namespace Tavstal.YggdrasilSharp.Services.Database;

/// <summary>
/// Provides functionality to initialize the custom database context.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Initializes the database by ensuring it is created and populating default data.
    /// </summary>
    /// <param name="context">The custom database context to initialize.</param>
    /// <param name="userStore">The custom user store used for managing user data.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public static async Task InitializeAsync(CustomDbContext context, CustomUserStore userStore, CancellationToken cancellationToken = default)
    {
        // Ensures the database is created.
        await context.Database.EnsureCreatedAsync(cancellationToken);
        
        // Checks if roles are empty and adds default roles.
        var roles = await userStore.Roles.QueryAsync(null, cancellationToken);
        if (!roles.Any())
        {
            await userStore.Roles.AddRangeAsync([
                new(1, "Default", "DEFAULT"),
                new (90, "Moderator", "MODERATOR"),
                new(100, "Admin", "ADMIN"),
            ], true, cancellationToken);

            Dictionary<string, CustomRole> roleCache = new();
                 
            // Adds claims to roles based on predefined role claims.
            foreach (var roleClaims in CustomRoleClaims.Claims)
            {
                if (!roleCache.TryGetValue(roleClaims.Key, out CustomRole? role))
                {
                    role = await userStore.Roles.FindAsync(x => x.NormalizedName == roleClaims.Key, cancellationToken);
                    if (role == null)
                        continue;
                    roleCache[roleClaims.Key] = role;
                }

                await userStore.RoleClaims.AddRangeAsync(RoleClaim.ToList(roleClaims.Value.ToList(), role.Id), cancellationToken: cancellationToken);
            }
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}