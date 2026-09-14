namespace Tavstal.YggdrasilSharp.Models.Database.User.Claims;

/// <summary>
/// Represents a user claim, which includes a key and a default value.
/// Provides methods to convert the user claim to a custom user claim and to create lists of custom user claims.
/// </summary>
public class UserClaim
{
    /// <summary>
    /// Gets or sets the key of the user claim.
    /// </summary>
    public string Key { get; set; }
    
    /// <summary>
    /// Gets or sets the default value of the user claim.
    /// </summary>
    public string DefaultValue { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserClaim"/> class with the specified key and default value.
    /// </summary>
    /// <param name="key">The key of the user claim.</param>
    /// <param name="value">The default value of the user claim.</param>
    public UserClaim(string key, string value) { Key = key; DefaultValue = value; }
    
    /// <summary>
    /// Converts the user claim to a <see cref="CustomUserClaim"/> using the specified user ID.
    /// </summary>
    /// <param name="userId">The ID of the user associated with the claim.</param>
    /// <returns>A <see cref="CustomUserClaim"/> object.</returns>
    public CustomUserClaim ToIdentityClaim(string userId)
    {
        return new CustomUserClaim
        {
            ClaimType = Key,
            ClaimValue = DefaultValue,
            UserId = userId
        };
    }
    
    /// <summary>
    /// Converts the user claim to a <see cref="CustomUserClaim"/> using the specified user.
    /// </summary>
    /// <param name="user">The user associated with the claim.</param>
    /// <returns>A <see cref="CustomUserClaim"/> object.</returns>
    public CustomUserClaim ToIdentityClaim(CustomUser user)
    {
        return ToIdentityClaim(user.Id);
    }
    
    /// <summary>
    /// Converts a list of <see cref="UserClaim"/> objects to a list of <see cref="CustomUserClaim"/> objects
    /// using the specified user ID.
    /// </summary>
    /// <param name="claims">The list of user claims to convert.</param>
    /// <param name="userId">The ID of the user associated with the claims.</param>
    /// <returns>A list of <see cref="CustomUserClaim"/> objects.</returns>
    public static List<CustomUserClaim> ToList(List<UserClaim> claims, string userId)
    {
        List<CustomUserClaim> local = new List<CustomUserClaim>();
        foreach (var claim in claims)
            local.Add(claim.ToIdentityClaim(userId));
        return local;
    }
    
    /// <summary>
    /// Converts a list of <see cref="UserClaim"/> objects to a list of <see cref="CustomUserClaim"/> objects
    /// using the specified user.
    /// </summary>
    /// <param name="claims">The list of user claims to convert.</param>
    /// <param name="user">The user associated with the claims.</param>
    /// <returns>A list of <see cref="CustomUserClaim"/> objects.</returns>
    public static List<CustomUserClaim> ToList(List<UserClaim> claims, CustomUser user)
    {
        return ToList(claims, user.Id);
    }
}
