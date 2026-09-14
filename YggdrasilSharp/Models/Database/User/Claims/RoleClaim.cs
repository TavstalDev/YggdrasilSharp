using Microsoft.AspNetCore.Identity;

namespace Tavstal.YggdrasilSharp.Models.Database.User.Claims;

/// <summary>
/// Represents a role claim, which includes a key and a default value.
/// Provides methods to convert the role claim to an IdentityRoleClaim and to create lists of IdentityRoleClaims.
/// </summary>
public class RoleClaim
{
    /// <summary>
    /// Gets the key of the role claim.
    /// </summary>
    public string Key { get; }
    
    /// <summary>
    /// Gets the default value of the role claim.
    /// </summary>
    public string DefaultValue { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleClaim"/> class with the specified key and default value.
    /// </summary>
    /// <param name="key">The key of the role claim.</param>
    /// <param name="value">The default value of the role claim.</param>
    public RoleClaim(string key, string value) 
    { 
        Key = key; 
        DefaultValue = value; 
    }
    
    /// <summary>
    /// Converts the role claim to an <see cref="IdentityRoleClaim{TKey}"/> using the specified role ID.
    /// </summary>
    /// <param name="roleId">The ID of the role associated with the claim.</param>
    /// <returns>An <see cref="IdentityRoleClaim{TKey}"/> object.</returns>
    public IdentityRoleClaim<string> ToIdentityClaim(string roleId)
    {
        return new IdentityRoleClaim<string>
        {
            ClaimType = Key,
            ClaimValue = DefaultValue,
            RoleId = roleId
        };
    }
    
    /// <summary>
    /// Converts the role claim to an <see cref="IdentityRoleClaim{TKey}"/> using the specified role.
    /// </summary>
    /// <param name="role">The role associated with the claim.</param>
    /// <returns>An <see cref="IdentityRoleClaim{TKey}"/> object.</returns>
    public IdentityRoleClaim<string> ToIdentityClaim(CustomRole role)
    {
        return ToIdentityClaim(role.Id);
    }
    
    /// <summary>
    /// Converts a list of <see cref="RoleClaim"/> objects to a list of <see cref="IdentityRoleClaim{TKey}"/> objects
    /// using the specified role ID.
    /// </summary>
    /// <param name="claims">The list of role claims to convert.</param>
    /// <param name="roleId">The ID of the role associated with the claims.</param>
    /// <returns>A list of <see cref="IdentityRoleClaim{TKey}"/> objects.</returns>
    public static List<IdentityRoleClaim<string>> ToList(List<RoleClaim> claims, string roleId)
    {
        List<IdentityRoleClaim<string>> local = new List<IdentityRoleClaim<string>>();
        foreach (var claim in claims)
            local.Add(claim.ToIdentityClaim(roleId));
        return local;
    }
    
    /// <summary>
    /// Converts a list of <see cref="RoleClaim"/> objects to a list of <see cref="IdentityRoleClaim{TKey}"/> objects
    /// using the specified role.
    /// </summary>
    /// <param name="claims">The list of role claims to convert.</param>
    /// <param name="role">The role associated with the claims.</param>
    /// <returns>A list of <see cref="IdentityRoleClaim{TKey}"/> objects.</returns>
    public static List<IdentityRoleClaim<string>> ToList(List<RoleClaim> claims, CustomRole role)
    {
        return ToList(claims, role.Id);
    }
}
