using System;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// Whether a store matches two key values exactly as the in-memory join and lookup do (FDW-6969). In memory, numbers match by value whatever their
/// type, text matches ignoring case, and anything else by its own equality; a store matches text by its collation and may convert between types,
/// so only keys whose types the two match the same way are pushed.
/// </summary>
public static class PushdownKeyTypes
{
    /// <summary>Judges a pair of key fields.</summary>
    /// <param name="left">The CLR type of the left key field's stated data type, or null.</param>
    /// <param name="right">The CLR type of the right key field's stated data type, or null.</param>
    /// <returns>Why the pair is not matched the same way in the store, or empty when it is.</returns>
    public static string Judge(Type? left, Type? right)
    {
        if (left is null || right is null)
            return "a key field states no data type";
        if (left == typeof(string) || right == typeof(string))
            return "a key is text, which the in-memory join matches ignoring case and a store matches by its collation";
        if (left == typeof(float) || left == typeof(double) || right == typeof(float) || right == typeof(double))
            return "a key is a floating-point number, which a store and the in-memory join may round differently";
        if (left == right || (IsExactNumber(left) && IsExactNumber(right)))
            return string.Empty;
        return $"its key types {left.Name} and {right.Name} differ, and a store may convert one to the other";
    }

    private static bool IsExactNumber(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint)
        || type == typeof(long) || type == typeof(ulong) || type == typeof(decimal);
}
