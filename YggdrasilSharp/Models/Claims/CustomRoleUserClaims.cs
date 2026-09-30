using Tavstal.YggdrasilSharp.Models.Database.User.Claims;

namespace Tavstal.YggdrasilSharp.Models.Claims;

/// <summary>
/// The default user claims, this class is used to generate the claims assigned to users during database creation.
/// </summary>
/// <remarks>
/// The dictionary is keyed by role name and maps to the <see cref="UserClaim"/> instances assigned to users holding
/// that role. A role mapped to an empty list means users with that role receive no user-specific claims.
/// </remarks>
public static class CustomRoleUserClaims
{
    private static readonly Dictionary<string, List<UserClaim>> _claims = new()
    {
        { "Default", [] },
        { "Moderator", [
                new(CustomClaimTypes.Badge, "Moderator")
            ]
        },
        { "Admin", [new(CustomClaimTypes.Badge, "Administrator")]
        },
    };

    /// <summary>
    /// Gets the default user claims keyed by role name.
    /// </summary>
    public static Dictionary<string, List<UserClaim>> Claims => _claims;
}