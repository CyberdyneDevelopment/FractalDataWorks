using System;
using Fdw.Collections.Attributes;

namespace Fdw.Collections;

/// <summary>
/// Base class for type option types that enforces Id, Name, and Category properties through constructor initialization.
/// This base class uses auto properties instead of abstract properties for cleaner code.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier. Must implement IEquatable for dictionary lookups.</typeparam>
/// <typeparam name="T">The derived type option type (CRTP pattern).</typeparam>
public abstract class TypeOptionBase<TKey, T> : ITypeOption<TKey, T>
    where TKey : IEquatable<TKey>
    where T : ITypeOption<TKey, T>
{
    /// <summary>
    /// Gets the unique identifier for this type option value.
    /// </summary>
    [TypeLookup("ById")]
    public virtual TKey Id { get; }

    /// <summary>
    /// Gets the unique identifier as object (explicit interface implementation).
    /// </summary>
    object ITypeOption.Id => Id;

    /// <summary>
    /// Gets the name of this type option value.
    /// </summary>
    [TypeLookup("ByName")]
    public string Name { get; }

    /// <summary>
    /// Backing field for the category value.
    /// </summary>
    private readonly string _category;

    /// <summary>
    /// Gets the category of this type option value.
    /// </summary>
    public string Category => string.IsNullOrEmpty(_category) ? "NotCategorized" : _category;

    /// <summary>
    /// Gets the configuration key for this type option value.
    /// Used for configuration lookups and service registration.
    /// </summary>
    public string ConfigurationKey { get; }

    /// <summary>
    /// Gets the display name for this type option value.
    /// Used for user-facing displays and documentation.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the description of this type option value.
    /// Provides detailed information about what this option does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeOptionBase{TKey, T}"/> class with default category.
    /// </summary>
    /// <param name="id">The unique identifier for this type option value.</param>
    /// <param name="name">The name of this type option value.</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    protected TypeOptionBase(TKey id, string name) : this(id, name, string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeOptionBase{TKey, T}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this type option value.</param>
    /// <param name="name">The name of this type option value.</param>
    /// <param name="category">The category of this type option value. Pass null or empty string for default "NotCategorized".</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    protected TypeOptionBase(TKey id, string name, string? category)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));

        Id = id;
        Name = name;
        _category = category ?? string.Empty;
        ConfigurationKey = $"TypeOptions:{name}";
        DisplayName = name;
        Description = $"Type option: {name}";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeOptionBase{TKey, T}"/> class with full metadata.
    /// </summary>
    /// <param name="id">The unique identifier for this type option value.</param>
    /// <param name="name">The name of this type option value.</param>
    /// <param name="configurationKey">The configuration key for service registration and lookups.</param>
    /// <param name="displayName">The display name for user-facing representations.</param>
    /// <param name="description">The detailed description of this type option.</param>
    /// <param name="category">The category of this type option value.</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    protected TypeOptionBase(TKey id, string name, string configurationKey, string displayName, string description, string? category)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));

        Id = id;
        Name = name;
        _category = category ?? string.Empty;
        ConfigurationKey = configurationKey ?? $"TypeOptions:{name}";
        DisplayName = displayName ?? name;
        Description = description ?? $"Type option: {name}";
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// Two type options are equal if they have the same Id of the same type.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not ITypeOption other)
            return false;

        // Check if the other's Id is the right type
        if (other.Id is not TKey otherTypedId)
            return false;

        // Use strongly-typed equality
        return Id.Equals(otherTypedId);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Returns a string representation of this type option (returns the Name property).
    /// </summary>
    public override string ToString() => Name;
}