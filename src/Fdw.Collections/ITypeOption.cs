using System;

namespace Fdw.Collections;

/// <summary>
/// Represents a type option that provides additional functionality for type collections.
/// This interface enables strongly-typed type options with identifiers, names, categories, and the ability to represent an empty state.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier. Must implement IEquatable for dictionary lookups.</typeparam>
/// <typeparam name="TValue">The implementing type, used for self-referencing generics pattern (CRTP).</typeparam>
/// <remarks>
/// Excluded from code coverage: Interface with no implementation code.
/// </remarks>
public interface ITypeOption<TKey, TValue> : ITypeOption<TKey>
    where TKey : IEquatable<TKey>
    where TValue : ITypeOption<TKey, TValue>
{
}

/// <summary>
/// Represents a type option with a strongly-typed key.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier. Must implement IEquatable for dictionary lookups.</typeparam>
/// <remarks>
/// Excluded from code coverage: Interface with no implementation code.
/// </remarks>
public interface ITypeOption<TKey> : ITypeOption
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets the strongly-typed unique identifier for this type option value.
    /// Hides the base interface's object Id property with strongly-typed version.
    /// </summary>
    new TKey Id { get; }
}

/// <summary>
/// Base interface for type options with object-typed Id for reflection/non-generic code.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Interface with no implementation code.
/// </remarks>
public interface ITypeOption
{
    /// <summary>
    /// Gets the unique identifier for this type option value (boxed as object).
    /// Use ITypeOption&lt;TKey&gt; for strongly-typed access.
    /// </summary>
    object Id { get; }

    /// <summary>
    /// Gets the display name or string representation of this type option value.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the category for this type option value.
    /// </summary>
    string Category { get; }
}
