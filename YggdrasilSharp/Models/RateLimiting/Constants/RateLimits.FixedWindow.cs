namespace Tavstal.YggdrasilSharp.Models.RateLimiting.Constants;

public static partial class RateLimits
{
    /// <summary>
    /// Contains the rate limit categories for fixed window limiting.
    /// </summary>
    public static class FixedWindow
    {
        /// <summary>
        /// Represents the rate limit category for user registration actions.
        /// </summary>
        public const string AUTH_REGISTER = "AuthRegister";

        /// <summary>
        /// Represents the rate limit category for user login actions.
        /// </summary>
        public const string AUTH_LOGIN = "AuthLogin";

        /// <summary>
        /// Represents the rate limit category for password reset actions.
        /// </summary>
        public const string AUTH_RESET = "AuthReset";

        /// <summary>
        /// Represents the rate limit category for file upload actions.
        /// </summary>
        public const string UPLOAD = "Upload";

        /// <summary>
        /// Represents the rate limit category for file download actions.
        /// </summary>
        public const string DOWNLOAD = "Download";

        /// <summary>
        /// Represents the rate limit category for search actions.
        /// </summary>
        public const string SEARCH = "Search";

        /// <summary>
        /// Represents the rate limit category for write actions.
        /// </summary>
        public const string WRITE = "Write";

        /// <summary>
        /// Represents the rate limit category for administrative actions.
        /// </summary>
        public const string ADMIN = "Admin";
    }
}