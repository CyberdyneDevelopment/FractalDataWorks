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
    /// Gets the unique identifier for this type option value.
    /// </summary>
    /// <remarks>
    /// Declared once, here, in the key type. No interface redeclares or hides it, and no option
    /// supplies it: an option keyed on int gets it from <c>TypeOptionBase&lt;TBase&gt;</c>, one keyed on
    /// Guid from <c>GuidTypeOptionBase&lt;TBase&gt;</c>, and both derive it from the option's fully
    /// qualified type name.
    /// </remarks>
    TKey Id { get; }
}

/// <summary>
/// Base interface for type options, for code that handles options of any key type.
/// </summary>
/// <remarks>
/// <para>
/// It carries no Id. An Id's type is its collection's key type, so it is declared on
/// <see cref="ITypeOption{TKey}"/>; declaring an object-typed one here as well meant every option
/// had two Ids, one hiding the other.
/// </para>
/// <para>Excluded from code coverage: Interface with no implementation code.</para>
/// </remarks>
public interface ITypeOption
{
    /// <summary>
    /// Gets the display name or string representation of this type option value.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the category for this type option value.
    /// </summary>
    string Category { get; }
}
