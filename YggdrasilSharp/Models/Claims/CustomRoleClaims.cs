using Tavstal.YggdrasilSharp.Models.Database.User.Claims;

namespace Tavstal.YggdrasilSharp.Models.Claims;

/// <summary>
/// The default role claims, this class is used to generate the claims during database creation.
/// </summary>
/// <remarks>
/// The dictionary is keyed by role name and maps to the <see cref="RoleClaim"/> instances granted by that role.
/// A role absent from the dictionary is granted no role claims.
/// </remarks>
public static class CustomRoleClaims
{
    private static readonly Dictionary<string, List<RoleClaim>> _claims = new()
    {
        { "Default", [
                new(CustomPermissions.Account.Create.Avatar, "true"),
                new(CustomPermissions.Account.Delete.Avatar, "true"),
                new(CustomPermissions.Account.Delete.Session, "true"),
                new(CustomPermissions.Account.Delete.Sessions, "true"),
                new(CustomPermissions.Account.View.Sessions, "true"),
                new (CustomPermissions.Account.View.Avatar, "true"),

                new(CustomPermissions.Capes.Select, "true"),
                new(CustomPermissions.Capes.Unselect, "true"),

                new(CustomPermissions.Skins.View, "true"),
                new(CustomPermissions.Skins.Upload, "true"),
                new(CustomPermissions.Skins.Delete, "true")
            ]
        },
        { "Moderator", [
            new RoleClaim(CustomPermissions.Account.Delete.AvatarOther, "true"),
            new RoleClaim(CustomPermissions.Skins.DeleteOther, "true"),
        ] },
        { "Admin", [
            new RoleClaim(CustomPermissions.Account.Create.AvatarOther, "true"),
            new RoleClaim(CustomPermissions.Account.Delete.AvatarOther, "true"),
            new RoleClaim(CustomPermissions.Account.Delete.SessionOther, "true"),
            new RoleClaim(CustomPermissions.Account.Delete.SessionsOther, "true"),
            new RoleClaim(CustomPermissions.Account.View.SessionsOther, "true"),
            
            new RoleClaim(CustomPermissions.Capes.Create, "true"),
            new RoleClaim(CustomPermissions.Capes.Delete, "true"),
            new RoleClaim(CustomPermissions.Capes.SelectOther, "true"),
            new RoleClaim(CustomPermissions.Capes.UnselectOther, "true"),
            
            new RoleClaim(CustomPermissions.Skins.ViewOther, "true"),
            new RoleClaim(CustomPermissions.Skins.UploadOther, "true"),
            new RoleClaim(CustomPermissions.Skins.DeleteOther, "true"),
            
            new RoleClaim(CustomPermissions.News.Create, "true"),
            new RoleClaim(CustomPermissions.News.Update, "true"),
            new RoleClaim(CustomPermissions.News.Delete, "true"),
            
            new RoleClaim(CustomPermissions.Launcher.CreateVersion, "true"),
            new RoleClaim(CustomPermissions.Launcher.UpdateVersion, "true"),
            new RoleClaim(CustomPermissions.Launcher.DeleteVersion, "true"),
        ] },
    };

    /// <summary>
    /// Gets the default role claims keyed by role name.
    /// </summary>
    public static Dictionary<string, List<RoleClaim>> Claims => _claims;
}