using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    /// Initializes the database by applying pending EF Core migrations and populating default data.
    /// </summary>
    /// <param name="context">The custom database context to initialize.</param>
    /// <param name="userStore">The custom user store used for managing user data.</param>
    /// <param name="passwordHasher">The password hasher used for hashing user passwords.</param>
    /// <param name="configuration">The configuration used for accessing application settings.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public static async Task InitializeAsync(CustomDbContext context, CustomUserStore userStore, IPasswordHasher<CustomUser> passwordHasher,
        IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        // Applies pending EF Core migrations, creating the schema if the database is empty.
        await context.Database.MigrateAsync(cancellationToken);

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

        var hasBeenUsersSetup = await userStore.ExistsUserAsync(x => x.EmailConfirmed, cancellationToken);
        if (!hasBeenUsersSetup)
        {
            string adminUsername = configuration[Constants.EnvironmentKeys.AdminUsername] ?? "admin";
            string adminEmail = configuration[Constants.EnvironmentKeys.AdminEmail] ?? "admin@localhost";
            string? adminPassword = configuration[Constants.EnvironmentKeys.AdminPassword];
            ArgumentException.ThrowIfNullOrWhiteSpace(adminPassword);
            var user = await userStore.FindUserAsync(x => x.NormalizedUserName == adminUsername.Normalize(), cancellationToken);
            if (user == null)
            {
                var adminUser = new CustomUser
                {
                    UserName = adminUsername,
                    NormalizedUserName = adminUsername.Normalize(),
                    Email = adminEmail,
                    NormalizedEmail = adminEmail.Normalize(),
                    PasswordHash = string.Empty,
                    EmailConfirmed = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    CreateDate = DateTimeOffset.UtcNow,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                adminUser = await userStore.AddUserAsync(adminUser, true, cancellationToken);
                adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);
                adminUser.SecurityStamp = Guid.NewGuid().ToString();
                adminUser.LastUpdate = DateTimeOffset.UtcNow;
                await userStore.UpdateUserAsync(adminUser, true, cancellationToken);

                var adminRole = await userStore.Roles.FindAsync(x => x.NormalizedName == "ADMIN", cancellationToken);
                if (adminRole != null)
                {
                    await userStore.UserRoles.AddAsync(new CustomUserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id
                    }, false, cancellationToken);
                    await userStore.UserClaims.AddAsync(new CustomUserClaim { UserId = adminUser.Id, ClaimType = ClaimTypes.Role, ClaimValue = adminRole.Name}, false, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}
