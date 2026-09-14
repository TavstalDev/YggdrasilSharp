#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Tavstal.YggdrasilSharp.Models.Claims;

// Static class containing all custom permissions used in the application.
public static class CustomPermissions
{
    public static class Account
    {
        public static class View
        {
            public const string Avatar = "ysharp.permission.account.view.avatar";
            public const string Sessions = "ysharp.permission.account.view.sessions";
            
            // Elevated Permissions
            public const string SessionsOther = "ysharp.permission.account.view.sessions.other";
        }
        
        public static class Create
        {
            public const string Avatar = "ysharp.permission.account.create.avatar";
            
            // Elevated Permissions
            public const string AvatarOther = "ysharp.permission.account.create.avatar.other";
        }
        
        public static class Update 
        {
            
        }
        
        public static class Delete
        {
            public const string Avatar = "ysharp.permission.account.delete.avatar";
            public const string Session = "ysharp.permission.account.delete.session";
            public const string Sessions = "ysharp.permission.account.delete.sessions";
            
            // Elevated Permissions
            public const string AvatarOther = "ysharp.permission.account.delete.avatar.other";
            public const string SessionOther = "ysharp.permission.account.delete.session.other";
            public const string SessionsOther = "ysharp.permission.account.delete.sessions.other";
        }
    }

    public static class Skins
    {
        public const string View = "ysharp.permission.skins.view";
        public const string Upload = "ysharp.permission.skins.upload";
        public const string Delete = "ysharp.permission.skins.delete";
        
        // Elevated Permissions
        public const string ViewOther = "ysharp.permission.skins.view.other";
        public const string UploadOther = "ysharp.permission.skins.upload.other";
        public const string DeleteOther = "ysharp.permission.skins.delete.other";
    }

    public static class Capes
    {
        public const string Select = "ysharp.permission.capes.select";
        public const string Unselect = "ysharp.permission.capes.unselect";
        
        // Elevated Permissions
        public const string Create = "ysharp.permission.capes.create";
        public const string Delete = "ysharp.permission.capes.delete";
        public const string SelectOther = "ysharp.permission.capes.select.other";
        public const string UnselectOther = "ysharp.permission.capes.unselect.other";
    }
    
    public static class News
    {
        public const string Create = "ysharp.permission.news.create";
        public const string Update = "ysharp.permission.news.update";
        public const string Delete = "ysharp.permission.news.delete";
    }

    public static class Launcher
    {
        public const string CreateVersion = "ysharp.permission.launcher.create_version";
        public const string UpdateVersion = "ysharp.permission.launcher.update_version";
        public const string DeleteVersion = "ysharp.permission.launcher.delete_version";
    }
}