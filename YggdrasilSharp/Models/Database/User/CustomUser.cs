using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tavstal.YggdrasilSharp.Models.Common;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace Tavstal.YggdrasilSharp.Models.Database.User;

/// <summary>
/// Represents a custom user entity that extends the IdentityUser class with additional properties
/// such as DisplayName, SkinModel, DiscordId, and various navigation properties.
/// </summary>
[Index(nameof(NormalizedUserName), IsUnique = true, Name = "UserNameIndex")]
[Index(nameof(NormalizedEmail), IsUnique = true, Name = "EmailIndex")]
public sealed class CustomUser : IdentityUser<string>
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [StringLength(36)]
    public override string Id { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [StringLength(320)]
    [ProtectedPersonalData]
    [EmailAddress]
    [JsonIgnore]
    public override string Email { get; set; }
    
    /// <summary>
    /// Gets or sets the normalized email address of the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [StringLength(320)]
    [ProtectedPersonalData]
    [JsonIgnore]
    public override string NormalizedEmail { get; set; }
    
    /// <summary>
    /// Gets or sets the hashed password of the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [StringLength(256)]
    [ProtectedPersonalData]
    [JsonIgnore]
    public override string PasswordHash { get; set; }
    
    /// <summary>
    /// Gets or sets the username of the user.
    /// </summary>
    [StringLength(16)]
    public override string UserName { get; set; }
    
    /// <summary>
    /// Gets or sets the normalized username of the user.
    /// </summary>
    [StringLength(16)]
    public override string NormalizedUserName { get; set; }
    
    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    [StringLength(16)]
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Gets or sets the skin model type of the user.
    /// </summary>
    public ESkinType SkinModel { get; set; }
    
    /// <summary>
    /// Gets or sets the Discord ID of the user.
    /// </summary>
    public ulong? DiscordId { get; set; }
    
    /// <summary>
    /// Gets or sets the reason for the user's lockout, if applicable.
    /// </summary>
    [StringLength(200)]
    public string? LockoutReason { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether two-factor authentication is enabled for the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public override bool TwoFactorEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the secret used for two-factor authentication.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [StringLength(256)]
    [JsonIgnore]
    public string? TwoFactorSecret { get; set; }
    
    /// <summary>
    /// Gets or sets the creation date of the user.
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }
    
    /// <summary>
    /// Gets or sets the last update date of the user.
    /// </summary>
    public DateTimeOffset LastUpdate { get; set; }
    
    /// <summary>
    /// Gets or sets the last login date of the user.
    /// </summary>
    public DateTimeOffset LastLogin { get; set; }

    #region JSON Ignored Identity Properties
    
    /// <summary>
    /// Gets or sets the security stamp of the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public override string? SecurityStamp { get; set; }

    /// <summary>
    /// Gets or sets the concurrency stamp of the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public override string? ConcurrencyStamp { get; set; }

    /// <summary>
    /// Gets or sets the phone number of the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public override string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the phone number is confirmed.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public override bool PhoneNumberConfirmed { get; set; }
    #endregion
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomUser"/> class.
    /// </summary>
    public CustomUser() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomUser"/> class with the specified properties.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="userName">The username of the user.</param>
    /// <param name="normalizedUserName">The normalized username of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="normalizedEmail">The normalized email address of the user.</param>
    /// <param name="password">The hashed password of the user.</param>
    /// <param name="skinModel">The skin model type of the user.</param>
    /// <param name="discordId">The Discord ID of the user.</param>
    /// <param name="lockoutReason">The reason for the user's lockout.</param>
    /// <param name="createDate">The creation date of the user.</param>
    /// <param name="lastUpdate">The last update date of the user.</param>
    /// <param name="lastLogin">The last login date of the user.</param>
    public CustomUser(string id, string userName, string normalizedUserName, string email, string normalizedEmail, string password, ESkinType skinModel, ulong? discordId, string? lockoutReason, DateTimeOffset createDate, DateTimeOffset lastUpdate, DateTimeOffset lastLogin)
    {
        Id = id;
        UserName = userName;
        NormalizedUserName = normalizedUserName;
        SkinModel = skinModel;
        DisplayName = userName; // Default display name is the username
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = password;
        DiscordId = discordId;
        LockoutReason = lockoutReason;
        CreateDate = createDate;
        LastUpdate = lastUpdate;
        LastLogin = lastLogin;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomUser"/> class with the specified properties.
    /// </summary>
    /// <param name="userName">The username of the user.</param>
    /// <param name="normalizedUserName">The normalized username of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="normalizedEmail">The normalized email address of the user.</param>
    /// <param name="password">The hashed password of the user.</param>
    /// <param name="skinModel">The skin model type of the user.</param>
    /// <param name="discordId">The Discord ID of the user.</param>
    /// <param name="lockoutReason">The reason for the user's lockout.</param>
    /// <param name="createDate">The creation date of the user.</param>
    /// <param name="lastUpdate">The last update date of the user.</param>
    /// <param name="lastLogin">The last login date of the user.</param>
    public CustomUser(string userName, string normalizedUserName, string email, string normalizedEmail, string password, ESkinType skinModel, ulong? discordId, string? lockoutReason, DateTimeOffset createDate, DateTimeOffset lastUpdate, DateTimeOffset lastLogin) : base(userName)
    {
        UserName = userName;
        NormalizedUserName = normalizedUserName;
        DisplayName = userName; // Default display name is the username
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = password;
        SkinModel = skinModel;
        DiscordId = discordId;
        LockoutReason = lockoutReason;
        CreateDate = createDate;
        LastUpdate = lastUpdate;
        LastLogin = lastLogin;
    }

    /// <summary>
    /// Gets the skin variant of the user based on the skin model.
    /// </summary>
    /// <returns>A string representing the skin variant.</returns>
    public string GetSkinVariant()
    {
        switch (SkinModel)
        {
            case ESkinType.WIDE:
                return "classic";
            case ESkinType.SLIM:
                return "slim";
        }
        return "classic";
    }
    
    /// <summary>
    /// Gets the highest role of the user based on role level.
    /// </summary>
    public string HighestRole => UserRoles.OrderByDescending(x => x.Role?.Level).FirstOrDefault()?.Role?.Name ?? "Anonymous";
    
    /// <summary>
    /// Gets or sets the claims associated with the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ICollection<CustomUserClaim> Claims { get; set; }
    
    /// <summary>
    /// Gets or sets the tokens associated with the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ICollection<CustomUserToken> Tokens { get; set; }
    
    /// <summary>
    /// Gets or sets the user logins associated with the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ICollection<CustomUserLogin> UserLogins { get; set; }
    
    /// <summary>
    /// Gets or sets the roles associated with the user.
    /// </summary>
    public ICollection<CustomUserRole> UserRoles { get; set; }
    
    /// <summary>
    /// Gets or sets the billing information of the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public UserBillingInformation? BillingInformation { get; set; }
    
    /// <summary>
    /// Gets or sets the play sessions associated with the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ICollection<UserPlaySession> PlaySessions { get; set; }
    
    /// <summary>
    /// Gets or sets the files associated with the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ICollection<FileData> Files { get; set; }
    
    /// <summary>
    /// Gets or sets the capes owned by the user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ICollection<UserCape> OwnedCapes { get; set; }

    /// <summary>
    /// Gets the avatar file data of the user, if available.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore] 
    public FileData? Avatar => Files.FirstOrDefault(x => x.Type == EFileDataType.PROFILE_PICTURE);
    
    /// <summary>
    /// Gets a value indicating whether the user has an avatar.
    /// </summary>
    public bool HasAvatar => Files.Any(x => x.Type == EFileDataType.PROFILE_PICTURE);
    
    /// <summary>
    /// Gets the skin file data of the user, if available.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public FileData? Skin => Files.FirstOrDefault(x => x.Type == EFileDataType.SKIN);

    /// <summary>
    /// Gets the cape file data of the user, if available.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore] 
    public FileData? Cape => OwnedCapes.FirstOrDefault(x => x.IsSelected)?.Cape?.FileData;
}
