using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;

namespace Fdw.SourceGenerators.Models;

/// <summary>
/// Contains metadata about a value in a collection.
/// Used as a base model for EnumValueInfo, MessageValueInfo, ServiceValueInfo, etc.
/// </summary>
/// <typeparam name="TId">The type used for the collection ID (int for Collections, Guid for ServiceTypes).</typeparam>
public class CollectionValueInfoModel<TId> : IInputInfoModel, IEquatable<CollectionValueInfoModel<TId>>
    where TId : struct
{
    private string _inputHash = string.Empty;
    private readonly IDictionary<string, string> _properties = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly ISet<string> _categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the fully qualified type name for this value.
    /// </summary>
    public string FullTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the short type name (without namespace) for this value.
    /// </summary>
    public string ShortTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for this value.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this value should be included in the collection.
    /// </summary>
    public bool Include { get; set; } = true;

    /// <summary>
    /// Gets or sets the ordering of this value within the collection.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets an optional description for this value.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this type is abstract.
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// Gets or sets whether this type is static.
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// Gets or sets whether this type is generic (has type parameters).
    /// Generic types cannot be instantiated without type arguments.
    /// </summary>
    public bool IsGenericType { get; set; }

    /// <summary>
    /// Gets a dictionary of additional properties for this value.
    /// </summary>
    public IDictionary<string, string> Properties => _properties;

    /// <summary>
    /// Gets a set of categories associated with this value.
    /// </summary>
    public ISet<string> Categories => _categories;

    /// <summary>
    /// Gets or sets the return type for this value.
    /// </summary>
    public string? ReturnType { get; set; }

    /// <summary>
    /// Gets or sets the namespace for the return type.
    /// </summary>
    public string? ReturnTypeNamespace { get; set; }

    /// <summary>
    /// Gets or sets whether to generate a factory method for this value.
    /// </summary>
    public bool? GenerateFactoryMethod { get; set; }

    /// <summary>
    /// Gets or sets the constructor information for this value.
    /// </summary>
    public IList<ConstructorInfo> Constructors { get; set; } = new List<ConstructorInfo>();

    /// <summary>
    /// Gets or sets the ID for this value.
    /// For Collections: int ID extracted from constructor.
    /// For ServiceTypes: Guid ID computed from TService fully qualified name.
    /// Used for dictionary key in generated collections.
    /// </summary>
    public TId? BaseConstructorId { get; set; }

    // Note: ISymbol removed per Roslyn cookbook - symbols are never equatable
    // Extract needed information to other properties instead

    /// <summary>
    /// Gets a hash string representing the contents of this value info model for incremental generation.
    /// </summary>
    public string InputHash
    {
        get
        {
            if (string.IsNullOrEmpty(_inputHash))
            {
                _inputHash = InputHashCalculator.CalculateHash(this);
            }

            return _inputHash;
        }
    }

    /// <summary>
    /// Writes the contents of this value info model to a TextWriter for hash generation.
    /// </summary>
    /// <param name="writer">The TextWriter to write to.</param>
    /// <exception cref="ArgumentNullException">Thrown when the writer is null.</exception>
    public void WriteToHash(TextWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer), "The TextWriter cannot be null.");
        }

        // Write basic properties
        writer.Write(FullTypeName);
        writer.Write(ShortTypeName);
        writer.Write(Name);
        writer.Write(Include);
        writer.Write(Order);
        writer.Write(Description ?? string.Empty);
        writer.Write(IsAbstract);
        writer.Write(IsStatic);
        writer.Write(IsGenericType);
        writer.Write(ReturnType ?? string.Empty);
        writer.Write(ReturnTypeNamespace ?? string.Empty);
        writer.Write(GenerateFactoryMethod?.ToString() ?? string.Empty);

        // Write properties
        foreach (var prop in Properties.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            writer.Write(prop.Key);
            writer.Write(prop.Value);
        }

        // Write categories
        foreach (var category in Categories.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
        {
            writer.Write(category);
        }

        // Write constructors
        foreach (var ctor in Constructors)
        {
            writer.Write(ctor.Accessibility.ToString());
            writer.Write(ctor.IsPrimary);
            foreach (var param in ctor.Parameters)
            {
                writer.Write(param.Name);
                writer.Write(param.TypeName);
                writer.Write(param.HasDefaultValue);
                writer.Write(param.DefaultValue ?? string.Empty);
                writer.Write(param.Namespace ?? string.Empty);
            }
        }
    }

    /// <summary>
    /// Determines whether the specified <see cref="CollectionValueInfoModel{TId}"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The CollectionValueInfoModel to compare with the current instance.</param>
    /// <returns>true if the specified CollectionValueInfoModel is equal to the current instance; otherwise, false.</returns>
    public bool Equals(CollectionValueInfoModel<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Compare by InputHash for efficient equality
        return string.Equals(InputHash, other.InputHash, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as CollectionValueInfoModel<TId>);

    /// <summary>
    /// Returns a hash code for this value info model.
    /// </summary>
    /// <returns>A hash code for the current value info model.</returns>
    public override int GetHashCode()
    {
        // Use the InputHash for consistent hash code
        return StringComparer.Ordinal.GetHashCode(InputHash);
    }

    /// <summary>
    /// Returns a string representation of this value info for debugging.
    /// </summary>
    public override string ToString()
    {
        return $@"CollectionValueInfoModel {{
  FullTypeName: {FullTypeName}
  ShortTypeName: {ShortTypeName}
  Name: {Name}
  Include: {Include}
  Order: {Order}
  Description: {Description}
  ReturnType: {ReturnType}
  GenerateFactoryMethod: {GenerateFactoryMethod}
  Constructors: {Constructors.Count}
  Properties: {Properties.Count}
  Categories: {Categories.Count}
}}";
    }
}

/// <summary>
/// Non-generic type alias for CollectionValueInfoModel using int IDs.
/// Provides backward compatibility for Collections and Messages generators.
/// </summary>
public class CollectionValueInfoModel : CollectionValueInfoModel<int>
{
}