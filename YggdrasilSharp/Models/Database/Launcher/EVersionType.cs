namespace Tavstal.YggdrasilSharp.Models.Database.Launcher;

/// <summary>
/// Represents the different version types of the launcher.
/// </summary>
public enum EVersionType
{
    /// <summary>
    /// Alpha version type, typically used for early testing phases.
    /// </summary>
    ALPHA = 0,

    /// <summary>
    /// Beta version type, used for testing with more features implemented.
    /// </summary>
    BETA = 1,

    /// <summary>
    /// Release version type, representing the final stable version.
    /// </summary>
    RELEASE = 2,
}
