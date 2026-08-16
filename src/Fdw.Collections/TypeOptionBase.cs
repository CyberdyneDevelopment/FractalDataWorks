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

/// <summary>
/// Base class for type option types keyed on <see cref="int"/>, the key type nearly every option
/// family uses. Spares an option from restating <c>TypeOptionBase&lt;int, T&gt;</c> at every
/// declaration site.
/// </summary>
/// <typeparam name="TBase">The derived type option type (CRTP pattern).</typeparam>
public abstract class TypeOptionBase<TBase> : TypeOptionBase<int, TBase>
    where TBase : ITypeOption<int, TBase>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeOptionBase{TBase}"/> class with default category.
    /// </summary>
    /// <param name="id">The unique identifier for this type option value.</param>
    /// <param name="name">The name of this type option value.</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    protected TypeOptionBase(int id, string name) : base(id, name)
    {
        _id = id;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeOptionBase{TBase}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this type option value.</param>
    /// <param name="name">The name of this type option value.</param>
    /// <param name="category">The category of this type option value. Pass null or empty string for default "NotCategorized".</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    protected TypeOptionBase(int id, string name, string? category) : base(id, name, category)
    {
        _id = id;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeOptionBase{TBase}"/> class with full metadata.
    /// </summary>
    /// <param name="id">The unique identifier for this type option value.</param>
    /// <param name="name">The name of this type option value.</param>
    /// <param name="configurationKey">The configuration key for service registration and lookups.</param>
    /// <param name="displayName">The display name for user-facing representations.</param>
    /// <param name="description">The detailed description of this type option.</param>
    /// <param name="category">The category of this type option value.</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    protected TypeOptionBase(int id, string name, string configurationKey, string displayName, string description, string? category)
        : base(id, name, configurationKey, displayName, description, category)
    {
        _id = id;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeOptionBase{TBase}"/> class, deriving its id
    /// from <paramref name="name"/> via a stable FNV-1a hash instead of taking one explicitly.
    /// </summary>
    /// <param name="name">The name of this type option value.</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    /// <remarks>
    /// A derived id is a hash of the name, so renaming an option changes its id and orphans any
    /// stored row that referenced the old one. That makes an option's name part of its contract
    /// once anything persists it.
    /// </remarks>
    protected TypeOptionBase(string name) : base(0, name)
    {
        _id = null;
    }

    private readonly int? _id;

    /// <inheritdoc />
    /// <remarks>
    /// Null means no id was given, so one is derived from the option's fully qualified type name.
    ///
    /// Why the FQN and not the name: this used to hash the name, and names repeat. "Query" is the
    /// name of MsSqlQueryTranslator, PostgreSqlQueryTranslator, SqliteQueryTranslator and
    /// QueryCommand — four types, one number. Eight names in this codebase are shared that way.
    ///
    /// Why nullable and not a zero test: zero is a real id. The collections' Empty/NotFound sentinel
    /// holds it, and a zero test would renumber the one option whose id is load-bearing.
    ///
    /// Why an override and not the base initializer: the concrete type is not available there —
    /// "this" is illegal in an initializer and TBase is the family base, the same for every option
    /// in it. GetType() here returns the option itself.
    /// </remarks>
    public override int Id => _id ?? GenerateIdFromName(GetType().FullName ?? GetType().Name);

    /// <summary>
    /// Derives a stable identifier from an option's name using FNV-1a.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <returns>A stable non-negative id.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    protected static int GenerateIdFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        unchecked
        {
            const int offset = (int)2166136261;
            const int prime = 16777619;
            var hash = offset;
            foreach (var c in name)
            {
                hash = (hash ^ c) * prime;
            }

            return hash & 0x7FFFFFFF;
        }
    }
}