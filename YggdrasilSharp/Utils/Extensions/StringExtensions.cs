using System.Globalization;

namespace Tavstal.YggdrasilSharp.Utils.Extensions;

/// <summary>
/// Provides extension methods for string operations, including case-insensitive comparisons,
/// validation checks, and formatting utilities.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Compares two strings for equality, ignoring case.
    /// </summary>
    /// <param name="str1">The first string to compare.</param>
    /// <param name="str2">The second string to compare.</param>
    /// <returns>True if the strings are equal ignoring case; otherwise, false.</returns>
    public static bool EqualsIgnoreCase(this string str1, string str2)
    {
        return string.Compare(str1, str2, StringComparison.OrdinalIgnoreCase) == 0;
    }
    
    /// <summary>
    /// Checks if a string contains a specified substring, ignoring case.
    /// </summary>
    /// <param name="str">The string to search within.</param>
    /// <param name="part">The substring to search for.</param>
    /// <returns>True if the substring is found; otherwise, false.</returns>
    public static bool ContainsIgnoreCase(this string str, string part)
    {
        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(str, part, CompareOptions.IgnoreCase) >= 0;
    }
    
    /// <summary>
    /// Determines whether a string is null or empty.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <returns>True if the string is null or empty; otherwise, false.</returns>
    public static bool IsNullOrEmpty(this string? str)
    {
        return string.IsNullOrEmpty(str);
    }
    
    /// <summary>
    /// Capitalizes the first letter of each word in the string.
    /// </summary>
    /// <param name="str">The string to capitalize.</param>
    /// <returns>The capitalized string.</returns>
    public static string Capitalize(this string str)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str.ToLowerInvariant());
    }
    
    /// <summary>
    /// Determines whether a string is a valid email address.
    /// </summary>
    /// <param name="str">The string to validate.</param>
    /// <returns>True if the string is a valid email address; otherwise, false.</returns>
    public static bool IsValidEmail(this string str)
    {
        if (str.IsNullOrEmpty())
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(str);
            return addr.Address == str;
        }
        catch (Exception)
        {
            return false;
        }
    }
}