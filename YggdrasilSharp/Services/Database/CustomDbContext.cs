using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.Launcher;
using Tavstal.YggdrasilSharp.Models.Database.Server;
using Tavstal.YggdrasilSharp.Models.Database.User;

// ReSharper disable UnusedMember.Local - DbContext has private DbSet properties that are used by EF Core but not directly accessed in code, so we disable the warning for unused members.
// ReSharper disable UnusedAutoPropertyAccessor.Local - The DbSet properties in the CustomDbContext are used by Entity Framework Core for database operations, but they are not accessed directly in the code. Therefore, we disable the warning for unused auto property accessors.

namespace Tavstal.YggdrasilSharp.Services.Database;

/// <summary>
/// Custom database context for the application, extending IdentityDbContext with custom user and role entities.
/// </summary>
public class CustomDbContext : DbContext
{
    private DbSet<CustomUser> Users { get; set; }
    
    private DbSet<CustomUserClaim> UserClaims { get; set; }
    
    private DbSet<CustomUserRole> UserRoles { get; set; }
    
    private DbSet<CustomRole> Roles { get; set; }
    
    private DbSet<CustomUserToken> UserTokens { get; set; }
    
    private DbSet<CustomUserLogin> UserLogins { get; set; }
    
    private DbSet<IdentityRoleClaim<string>> RoleClaims { get; set; }

    private DbSet<UserBackupCode> UserBackupCodes { get; set; }
    
    private DbSet<UserBillingInformation> UserBillingInformations { get; set; }
    
    private DbSet<UserPlaySession> UserPlaySessions { get; set; }
    
    private DbSet<FileData> Files { get; set; }

    private DbSet<Cape> Capes { get; set; }
    
    private DbSet<UserCape> UserCapes { get; set; }
    
    private DbSet<ServerJoin> ServerJoins { get; set; }
    
    private DbSet<News> News { get; set; }
    
    private DbSet<LauncherVersion> LauncherVersions { get; set; }
    
    private DbSet<LauncherVersionData> LauncherVersionDatas { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    public CustomDbContext(DbContextOptions<CustomDbContext> options) : base(options) { }

    /// <summary>
    /// Configures the schema needed for the identity framework.
    /// </summary>
    /// <param name="builder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CustomUserClaim>()
            .HasOne(c => c.User) // Navigation property in IdentityUserClaim
            .WithMany(u => u.Claims) // Navigation property in CustomUser
            .HasForeignKey(c => c.UserId) // Foreign key
            .IsRequired(); // Ensure the relationship is required

        builder.Entity<CustomUserToken>()
            .HasOne(t => t.User) // Navigation property in CustomUserToken
            .WithMany(u => u.Tokens) // Navigation property in CustomUser
            .HasForeignKey(t => t.UserId) // Foreign key to CustomUser
            .IsRequired(); // Ensure the relationship is required

        builder.Entity<CustomUserToken>()
            .HasKey(x => x.Id);

        builder.Entity<CustomUserToken>()
            .Property(x => x.Value)
            .HasDefaultValueSql("UUID()")
            .ValueGeneratedOnAdd();
        
        builder.Entity<CustomUserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();

            entity.ToTable("AspNetUserRoles");
        });

        // Ensure that the foreign key is correctly mapped
        builder.Entity<CustomUserLogin>(entity =>
        {
            entity.HasKey(cul => cul.Id);
            entity.Property(cul => cul.Id)
                .ValueGeneratedOnAdd();

            entity.HasOne(cul => cul.User)
                .WithMany(cu => cu.UserLogins)
                .HasForeignKey(cul => cul.UserId)
                .IsRequired();
            
            entity.ToTable("AspNetUserLogins"); 
        });

        builder.Entity<UserBillingInformation>()
            .HasOne(b => b.User)
            .WithOne(u => u.BillingInformation)
            .HasForeignKey<UserBillingInformation>(b => b.UserId)
            .IsRequired();
        
        builder.Entity<UserPlaySession>()
            .HasOne(p => p.User)
            .WithMany(u => u.PlaySessions) 
            .HasForeignKey(p => p.UserId) 
            .IsRequired();
        
        builder.Entity<FileData>()
            .HasOne(f => f.User)
            .WithMany(u => u.Files)
            .HasForeignKey(f => f.UserId)
            .IsRequired(false);

        builder.Entity<UserCape>()
            .HasOne(c => c.User)
            .WithMany(u => u.OwnedCapes)
            .HasForeignKey(c => c.UserId)
            .IsRequired();
    }


    /// <summary>
    /// Configures the database (and other options) to be used for this context.
    /// </summary>
    /// <param name="optionsBuilder">A builder used to create or modify options for this context.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;
        Database.EnsureCreated();
    }

    /// <summary>
    /// Discards all changes made to the tracked entities.
    /// </summary>
    public void DiscardChanges()
    {
        ChangeTracker.Clear();
    }

    /// <summary>
    /// Clears expired user logins and their associated tokens from the database asynchronously.
    /// </summary>
    /// <param name="shouldSave">Indicates whether to save changes to the database after clearing the expired logins and tokens.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ClearExpiredUserLoginsAsync(bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        var expiredLogins = await UserLogins.Where(l => l.ExpireDate <= DateTimeOffset.UtcNow).ToListAsync(cancellationToken: cancellationToken);
        foreach (var login in expiredLogins)
        {
            var token = await UserTokens.FirstOrDefaultAsync(x => x.Id == login.ProviderKey, cancellationToken: cancellationToken);
            if (token != null)
                UserTokens.Remove(token);
        }
        
        UserLogins.RemoveRange(expiredLogins);
        if (shouldSave) await SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Clears expired user play sessions from the database asynchronously.
    /// </summary>
    /// <param name="shouldSave">Indicates whether to save changes to the database after clearing the expired sessions.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ClearExpiredUserPlaySessionsAsync(bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        var expiredSessions = await UserPlaySessions.Where(s => s.ExpiresAt <= DateTimeOffset.UtcNow).ToListAsync(cancellationToken: cancellationToken);
        UserPlaySessions.RemoveRange(expiredSessions);
        if (shouldSave) await SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Clears all user logins and tokens associated with the specified user ID.
    /// </summary>
    /// <param name="userId">The ID of the user whose logins and tokens are to be cleared.</param>
    /// <param name="shouldSave">Indicates whether to save changes to the database after clearing the logins and tokens.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ClearUserLoginsAsync(string userId, bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        var logins = await UserLogins.Where(x => x.UserId == userId).ToListAsync(cancellationToken: cancellationToken);
        var tokens = await UserTokens.Where(x => x.UserId == userId).ToListAsync(cancellationToken: cancellationToken);

        UserLogins.RemoveRange(logins);
        UserTokens.RemoveRange(tokens);
        
        if (shouldSave)
            await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Clears expired server join records from the database asynchronously.
    /// </summary>
    /// <param name="shouldSave">Indicates whether to save changes to the database after clearing the expired records.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ClearExpiredServerJoinsAsync(bool shouldSave = false, CancellationToken cancellationToken = default)
    {
        var expiredJoins = await ServerJoins.Where(sj => sj.ExpiresAt <= DateTimeOffset.UtcNow).ToListAsync(cancellationToken: cancellationToken);
        ServerJoins.RemoveRange(expiredJoins);
        if (shouldSave) await SaveChangesAsync(cancellationToken);
    }
}