namespace Tavstal.YggdrasilSharp.Utils.Extensions;

/// <summary>
/// Provides extension methods for arrays and lists, including index validation, shuffling, and value retrieval.
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// Checks if the specified index is valid for the given list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to check the index against.</param>
    /// <param name="index">The index to validate.</param>
    /// <returns>True if the index is valid; otherwise, false.</returns>
    public static bool IsValidIndex<T>(this List<T>? list, int index)
    {
        if (list == null)
            return false;
        return list.Count - 1 >= index;
    }
    
    /// <summary>
    /// Checks if the specified index is valid for the given array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="list">The array to check the index against.</param>
    /// <param name="index">The index to validate.</param>
    /// <returns>True if the index is valid; otherwise, false.</returns>
    public static bool IsValidIndex<T>(this T[]? list, int index)
    {
        if (list == null)
            return false;
        return list.Length - 1 >= index;
    }
    
    /// <summary>
    /// Shuffles the elements of the given array in a random order.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="list">The array to shuffle.</param>
    public static void Shuffle<T>(this T[] list)
    {
        int count = list.Length;
        while (count > 0)
        {
            count--;
            int index = MathExtensions.Next(count + 1);
            (list[index], list[count]) = (list[count], list[index]);
        }
    }
    
    /// <summary>
    /// Shuffles the elements of the given list in a random order.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to shuffle.</param>
    public static void Shuffle<T>(this List<T> list)
    {
        int count = list.Count;
        while (count > 0)
        {
            count--;
            int index = MathExtensions.Next(count + 1);
            (list[index], list[count]) = (list[count], list[index]);
        }
    }
    
    /// <summary>
    /// Attempts to find a value in the list that matches the specified predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to search.</param>
    /// <param name="match">The predicate to match against.</param>
    /// <param name="value">The value that matches the predicate, if found.</param>
    /// <returns>True if a matching value is found; otherwise, false.</returns>
    public static bool TryGetValue<T>(this List<T> list, Predicate<T> match, out T? value)
    {
        value = list.Find(match);
        return value != null;
    }
}