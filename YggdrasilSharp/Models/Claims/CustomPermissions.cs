#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Tavstal.YggdrasilSharp.Models.Claims;

// Static class containing all custom permissions used in the application.
public static class CustomPermissions
{
    public static class Account
    {
        public static class View
        {
            public const string Avatar = "mmc.permission.account.view.avatar";
            public const string Sessions = "mmc.permission.account.view.sessions";
            
            // Elevated Permissions
            public const string SessionsOther = "mmc.permission.account.view.sessions.other";
        }
        
        public static class Create
        {
            public const string Avatar = "mmc.permission.account.create.avatar";
            
            // Elevated Permissions
            public const string AvatarOther = "mmc.permission.account.create.avatar.other";
        }
        
        public static class Update 
        {
            
        }
        
        public static class Delete
        {
            public const string Avatar = "mmc.permission.account.delete.avatar";
            public const string Session = "mmc.permission.account.delete.session";
            public const string Sessions = "mmc.permission.account.delete.sessions";
            
            // Elevated Permissions
            public const string AvatarOther = "mmc.permission.account.delete.avatar.other";
            public const string SessionOther = "mmc.permission.account.delete.session.other";
            public const string SessionsOther = "mmc.permission.account.delete.sessions.other";
        }
    }

    public static class Skins
    {
        public const string View = "mmc.permission.skins.view";
        public const string Upload = "mmc.permission.skins.upload";
        public const string Delete = "mmc.permission.skins.delete";
        
        // Elevated Permissions
        public const string ViewOther = "mmc.permission.skins.view.other";
        public const string UploadOther = "mmc.permission.skins.upload.other";
        public const string DeleteOther = "mmc.permission.skins.delete.other";
    }

    public static class Capes
    {
        public const string Select = "mmc.permission.capes.select";
        public const string Unselect = "mmc.permission.capes.unselect";
        
        // Elevated Permissions
        public const string Create = "mmc.permission.capes.create";
        public const string Delete = "mmc.permission.capes.delete";
        public const string SelectOther = "mmc.permission.capes.select.other";
        public const string UnselectOther = "mmc.permission.capes.unselect.other";
    }
    
    public static class News
    {
        public const string Create = "mmc.permission.news.create";
        public const string Update = "mmc.permission.news.update";
        public const string Delete = "mmc.permission.news.delete";
    }

    public static class Launcher
    {
        public const string CreateVersion = "mmc.permission.launcher.create_version";
        public const string UpdateVersion = "mmc.permission.launcher.update_version";
        public const string DeleteVersion = "mmc.permission.launcher.delete_version";
    }
}