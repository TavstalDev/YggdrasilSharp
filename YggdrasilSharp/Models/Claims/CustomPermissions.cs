namespace Tavstal.YggdrasilSharp.Models.Claims;

/// <summary>
/// Static class containing all custom permissions used in the application.
/// </summary>
/// <remarks>
/// Permissions are grouped by resource and then by the action that may be performed on it.
/// Permissions whose name is suffixed with <c>Other</c> are elevated permissions that grant access to
/// the resources of other users, and typically require a moderator or administrator role.
/// </remarks>
public static class CustomPermissions
{
    /// <summary>
    /// Permissions related to user accounts, such as avatars and sessions.
    /// </summary>
    public static class Account
    {
        /// <summary>
        /// Permissions related to viewing account data.
        /// </summary>
        public static class View
        {
            /// <summary>
            /// Permission to view the avatar of the current user.
            /// </summary>
            public const string Avatar = "ysharp.permission.account.view.avatar";

            /// <summary>
            /// Permission to view the sessions of the current user.
            /// </summary>
            public const string Sessions = "ysharp.permission.account.view.sessions";

            /// <summary>
            /// Elevated permission to view the sessions of other users.
            /// </summary>
            public const string SessionsOther = "ysharp.permission.account.view.sessions.other";
        }

        /// <summary>
        /// Permissions related to creating account data.
        /// </summary>
        public static class Create
        {
            /// <summary>
            /// Permission to create or replace the avatar of the current user.
            /// </summary>
            public const string Avatar = "ysharp.permission.account.create.avatar";

            /// <summary>
            /// Elevated permission to create or replace the avatar of other users.
            /// </summary>
            public const string AvatarOther = "ysharp.permission.account.create.avatar.other";
        }

        /// <summary>
        /// Permissions related to updating account data.
        /// </summary>
        /// <remarks>This group is reserved for future use and currently defines no permissions.</remarks>
        public static class Update 
        {
            
        }

        /// <summary>
        /// Permissions related to deleting account data.
        /// </summary>
        public static class Delete
        {
            /// <summary>
            /// Permission to delete the avatar of the current user.
            /// </summary>
            public const string Avatar = "ysharp.permission.account.delete.avatar";

            /// <summary>
            /// Permission to delete a single session of the current user.
            /// </summary>
            public const string Session = "ysharp.permission.account.delete.session";

            /// <summary>
            /// Permission to delete all sessions of the current user.
            /// </summary>
            public const string Sessions = "ysharp.permission.account.delete.sessions";

            /// <summary>
            /// Elevated permission to delete the avatar of other users.
            /// </summary>
            public const string AvatarOther = "ysharp.permission.account.delete.avatar.other";

            /// <summary>
            /// Elevated permission to delete a single session of other users.
            /// </summary>
            public const string SessionOther = "ysharp.permission.account.delete.session.other";

            /// <summary>
            /// Elevated permission to delete all sessions of other users.
            /// </summary>
            public const string SessionsOther = "ysharp.permission.account.delete.sessions.other";
        }
    }

    /// <summary>
    /// Permissions related to Minecraft skins.
    /// </summary>
    public static class Skins
    {
        /// <summary>
        /// Permission to view the skins of the current user.
        /// </summary>
        public const string View = "ysharp.permission.skins.view";

        /// <summary>
        /// Permission to upload a skin for the current user.
        /// </summary>
        public const string Upload = "ysharp.permission.skins.upload";

        /// <summary>
        /// Permission to delete the skin of the current user.
        /// </summary>
        public const string Delete = "ysharp.permission.skins.delete";

        /// <summary>
        /// Elevated permission to view the skins of other users.
        /// </summary>
        public const string ViewOther = "ysharp.permission.skins.view.other";

        /// <summary>
        /// Elevated permission to upload a skin for other users.
        /// </summary>
        public const string UploadOther = "ysharp.permission.skins.upload.other";

        /// <summary>
        /// Elevated permission to delete the skin of other users.
        /// </summary>
        public const string DeleteOther = "ysharp.permission.skins.delete.other";
    }

    /// <summary>
    /// Permissions related to Minecraft capes.
    /// </summary>
    public static class Capes
    {
        /// <summary>
        /// Permission to equip a cape on the current user.
        /// </summary>
        public const string Select = "ysharp.permission.capes.select";

        /// <summary>
        /// Permission to unequip the cape of the current user.
        /// </summary>
        public const string Unselect = "ysharp.permission.capes.unselect";

        /// <summary>
        /// Elevated permission to create a cape and add it to the cape collection.
        /// </summary>
        public const string Create = "ysharp.permission.capes.create";

        /// <summary>
        /// Elevated permission to delete a cape from the cape collection.
        /// </summary>
        public const string Delete = "ysharp.permission.capes.delete";

        /// <summary>
        /// Elevated permission to equip a cape on other users.
        /// </summary>
        public const string SelectOther = "ysharp.permission.capes.select.other";

        /// <summary>
        /// Elevated permission to unequip the cape of other users.
        /// </summary>
        public const string UnselectOther = "ysharp.permission.capes.unselect.other";
    }

    /// <summary>
    /// Permissions related to news entries.
    /// </summary>
    public static class News
    {
        /// <summary>
        /// Permission to create a news entry.
        /// </summary>
        public const string Create = "ysharp.permission.news.create";

        /// <summary>
        /// Permission to update a news entry.
        /// </summary>
        public const string Update = "ysharp.permission.news.update";

        /// <summary>
        /// Permission to delete a news entry.
        /// </summary>
        public const string Delete = "ysharp.permission.news.delete";
    }

    /// <summary>
    /// Permissions related to launcher versions.
    /// </summary>
    public static class Launcher
    {
        /// <summary>
        /// Permission to create a launcher version.
        /// </summary>
        public const string CreateVersion = "ysharp.permission.launcher.create_version";

        /// <summary>
        /// Permission to update a launcher version.
        /// </summary>
        public const string UpdateVersion = "ysharp.permission.launcher.update_version";

        /// <summary>
        /// Permission to delete a launcher version.
        /// </summary>
        public const string DeleteVersion = "ysharp.permission.launcher.delete_version";
    }
}
