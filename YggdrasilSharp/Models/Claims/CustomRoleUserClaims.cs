using Tavstal.YggdrasilSharp.Models.Database.User.Claims;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Tavstal.YggdrasilSharp.Models.Claims;

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

    public static Dictionary<string, List<UserClaim>> Claims => _claims;
}