using System;

namespace Fdw.Abstractions;

/// <summary>
/// Reads a type with any <see cref="Nullable{T}"/> wrapper taken off (FDW-3580).
/// </summary>
/// <remarks>
/// Why one member: <c>Nullable.GetUnderlyingType(type) ?? type</c> was written at every mapper and reads as a fallback. It is
/// not one: a type that is not <c>Nullable&lt;T&gt;</c> is already its own underlying type, so the answer is the type itself.
/// </remarks>
public static class NullableTypeExtensions
{
    /// <summary>
    /// Gets <paramref name="type"/> without its <see cref="Nullable{T}"/> wrapper: <c>int</c> for <c>int?</c>, and the type itself
    /// for any type that is not <c>Nullable&lt;T&gt;</c>.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The type without <see cref="Nullable{T}"/>.</returns>
    public static Type WithoutNullable(this Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return Nullable.GetUnderlyingType(type) is { } underlying ? underlying : type;
    }
}
