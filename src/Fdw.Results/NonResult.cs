using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Results;

/// <summary>
/// Represents a unit type for operations that don't return a value.
/// Similar to void but can be used as a generic type parameter.
/// </summary>
/// <ExcludeFromTest>Simple unit type with no business logic to test</ExcludeFromTest>
[ExcludeFromCodeCoverage]
public struct NonResult : IEquatable<NonResult>
{
    /// <summary>
    /// Gets the default NonResult value.
    /// </summary>
    public static readonly NonResult Value;

    /// <summary>
    /// Determines whether this instance is equal to another NonResult.
    /// </summary>
    /// <param name="other">The other NonResult to compare.</param>
    /// <returns>Always returns true since all NonResult instances are equal.</returns>
    public bool Equals(NonResult other) => true;

    /// <summary>
    /// Determines whether this instance is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the object is a NonResult; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is NonResult;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>Always returns 0 since all NonResult instances are equal.</returns>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Returns a string representation of the NonResult.
    /// </summary>
    /// <returns>A string indicating this is a NonResult.</returns>
    public override string ToString() => nameof(NonResult);

    /// <summary>
    /// Determines whether two NonResult instances are equal.
    /// </summary>
    /// <param name="left">The first NonResult.</param>
    /// <param name="right">The second NonResult.</param>
    /// <returns>Always returns true.</returns>
    public static bool operator ==(NonResult left, NonResult right) => true;

    /// <summary>
    /// Determines whether two NonResult instances are not equal.
    /// </summary>
    /// <param name="left">The first NonResult.</param>
    /// <param name="right">The second NonResult.</param>
    /// <returns>Always returns false.</returns>
    public static bool operator !=(NonResult left, NonResult right) => false;
}
