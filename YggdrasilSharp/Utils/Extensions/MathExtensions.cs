namespace Tavstal.YggdrasilSharp.Utils.Extensions;

/// <summary>
/// Provides extension methods for mathematical operations, including random number generation,
/// clamping values, and formatting decimals.
/// </summary>
public static class MathExtensions
{
    /// <summary>
    /// A shared instance of the Random class for generating random numbers.
    /// </summary>
    private static readonly Random Rnd = new();

    /// <summary>
    /// A lock object to ensure thread safety during random number generation.
    /// </summary>
    private static readonly Lock _lock = new();

    /// <summary>
    /// Generates a random integer that is less than the specified maximum value.
    /// </summary>
    /// <param name="max">The exclusive upper bound of the random number to generate.</param>
    /// <returns>A random integer that is less than <paramref name="max"/>.</returns>
    public static int Next(int max)
    {
        lock (_lock)
        {
            return Rnd.Next(max);
        }
    }

    /// <summary>
    /// Generates a random integer within the specified range.
    /// </summary>
    /// <param name="min">The inclusive lower bound of the random number to generate.</param>
    /// <param name="max">The exclusive upper bound of the random number to generate.</param>
    /// <returns>A random integer that is greater than or equal to <paramref name="min"/> and less than <paramref name="max"/>.</returns>
    public static int Next(int min, int max)
    {
        lock (_lock)
        {
            return Rnd.Next(min, max);
        }
    }

    /// <summary>
    /// Clamps the integer value to a maximum value.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="maxValue">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    public static int Clamp(this int value, int maxValue)
    {
        return maxValue < value ? maxValue : value;
    }

    /// <summary>
    /// Clamps the integer value to a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="minValue">The minimum value.</param>
    /// <param name="maxValue">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    public static int Clamp(this int value, int minValue, int maxValue)
    {
        return minValue > value ? minValue : maxValue < value ? maxValue : value;
    }

    /// <summary>
    /// Clamps the decimal value to a maximum value.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="maxValue">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    public static decimal Clamp(this decimal value, decimal maxValue)
    {
        return maxValue < value ? maxValue : value;
    }

    /// <summary>
    /// Clamps the decimal value to a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="minValue">The minimum value.</param>
    /// <param name="maxValue">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    public static decimal Clamp(this decimal value, decimal minValue, decimal maxValue)
    {
        return minValue > value ? minValue : maxValue < value ? maxValue : value;
    }

    /// <summary>
    /// Converts the decimal value to a string with the specified format and replaces commas with dots.
    /// </summary>
    /// <param name="value">The decimal value to format.</param>
    /// <param name="format">The format string (default is "0.00").</param>
    /// <returns>The formatted string representation of the decimal value.</returns>
    public static string ToDecimalString(this decimal value, string format = "0.00")
    {
        return value.ToString(format).Replace(",", ".");
    }
}
