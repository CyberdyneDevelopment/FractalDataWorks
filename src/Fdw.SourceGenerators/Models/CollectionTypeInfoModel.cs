using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;

namespace Fdw.SourceGenerators.Models;

/// <summary>
/// Contains metadata about a collection type definition to be processed by source generators.
/// Used as a base model for EnumTypeInfo, MessageTypeInfo, ServiceTypeInfo, etc.
/// </summary>
/// <typeparam name="TId">The type used for collection IDs (int for Collections, Guid for ServiceTypes).</typeparam>
public class CollectionTypeInfoModel<TId> : IInputInfoModel, IEquatable<CollectionTypeInfoModel<TId>>
    where TId : struct
{
    // Note: ISymbol removed per Roslyn cookbook - symbols are never equatable
    // All needed information is extracted to other properties

    /// <summary>
    /// Gets or sets the namespace for the generated code.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the class name of the collection type.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully qualified type name of the base type.
    /// </summary>
    public string FullTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the type is generic.
    /// </summary>
    public bool IsGenericType { get; set; }

    /// <summary>
    /// Gets or sets the name of the generated collection class.
    /// </summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base type for collection-first pattern (extracted from generic parameter).
    /// For example, in ColorsCollectionBase&lt;ColorOption&gt;, this would be "ColorOption".
    /// </summary>
    public string? CollectionBaseType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate factory methods for values.
    /// </summary>
    public bool GenerateFactoryMethods { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to generate the collection class as static.
    /// When true, generates a static class. When false, generates an instance class.
    /// Defaults to true for source generators, but can be set to false for manual usage scenarios.
    /// </summary>
    public bool GenerateStaticCollection { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to generate a generic collection class.
    /// When true, generates Collections&lt;T&gt; where T : BaseType.
    /// When false, generates non-generic Collections class.
    /// Defaults to false.
    /// </summary>
    public bool Generic { get; set; }

    /// <summary>
    /// Gets or sets the generation strategy name to use.
    /// </summary>
    public string Strategy { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the collection strategy determining dictionary types and generation patterns.
    /// Immutable uses FrozenDictionary, Mutable uses ConcurrentDictionary, Factory uses Dictionary with Register().
    /// </summary>
    public CollectionStrategy CollectionStrategy { get; set; } = CollectionStrategy.Immutable;

    /// <summary>
    /// Gets or sets the string comparison mode for name-based lookups.
    /// </summary>
    public StringComparison NameComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Gets or sets a value indicating whether to include options from referenced assemblies.
    /// </summary>
    public bool IncludeReferencedAssemblies { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate methods for access instead of properties.
    /// When true, generates methods like <c>Opening()</c> instead of properties like <c>Opening</c>.
    /// Default is false (generates properties).
    /// </summary>
    public bool UseMethods { get; set; }

    /// <summary>
    /// Gets or sets how options should be stored internally for lookups.
    /// True provides O(1) lookups using Dictionary.
    /// False provides O(n) lookups using linear search.
    /// Defaults to true (Dictionary).
    /// </summary>
    public bool UseDictionaryStorage { get; set; } = true;

    /// <summary>
    /// Gets or sets the key type (TKey) for TypeCollections.
    /// Used for dictionary lookups and must implement IEquatable.
    /// Extracted from ITypeOption&lt;TKey, TSelf&gt; implementation.
    /// Stored as string (fully qualified type name) per Roslyn cookbook.
    /// </summary>
    public string? KeyType { get; set; }

    /// <summary>
    /// Gets or sets whether this TypeCollection is a parent collection (has children with MemberOf pointing to it).
    /// Parent collections generate Dictionary&lt;TKey, TReturnType&gt; and Register() method.
    /// </summary>
    public bool IsParentCollection { get; set; }


    /// <summary>
    /// Gets or sets the MemberOf parent collection type name if this is a child collection.
    /// Child collections generate static constructor that calls Parent.Register(Key, this).
    /// </summary>
    public string? MemberOfParent { get; set; }

    /// <summary>
    /// Gets or sets the return type for generated static properties and methods.
    /// If null, will be auto-detected based on implemented interfaces.
    /// </summary>
    public string? ReturnType { get; set; }

    /// <summary>
    /// Gets or sets the namespace to import for the return type.
    /// If null, will be extracted from ReturnType.
    /// </summary>
    public string? ReturnTypeNamespace { get; set; }

    /// <summary>
    /// Gets or sets the list of type parameters for generic types.
    /// </summary>
    public IList<string> TypeParameters { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of type constraints for generic types.
    /// </summary>
    public IList<string> TypeConstraints { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the unbound type name for generic types (e.g., "MyType`2").
    /// </summary>
    public string UnboundTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespaces required by generic constraints.
    /// </summary>
    public ISet<string> RequiredNamespaces { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the default generic return type from the attribute.
    /// </summary>
    public string? DefaultGenericReturnType { get; set; }

    /// <summary>
    /// Gets or sets the namespace for the default generic return type.
    /// </summary>
    public string? DefaultGenericReturnTypeNamespace { get; set; }

    /// <summary>
    /// Gets the list of properties that should be used for lookup methods.
    /// </summary>
    public EquatableArray<PropertyLookupInfoModel> LookupProperties { get; set; } = EquatableArray.Empty<PropertyLookupInfoModel>();

    /// <summary>
    /// Gets the list of concrete value types discovered during processing.
    /// </summary>
    public EquatableArray<CollectionValueInfoModel<TId>> ConcreteTypes { get; set; } = EquatableArray.Empty<CollectionValueInfoModel<TId>>();

    /// <summary>
    /// Gets or sets a value indicating whether the collection class inherits from a CollectionBase&lt;T&gt;.
    /// When true, the generator will only generate the necessary abstract method implementations.
    /// </summary>
    public bool InheritsFromCollectionBase { get; set; }

    /// <summary>
    /// Gets or sets the full name of the collection class (for collection-first pattern).
    /// </summary>
    public string? CollectionClassName { get; set; }

    /// <summary>
    /// Gets or sets the target framework for the compilation (e.g., "net8.0", "netstandard2.0").
    /// Used to conditionally generate code based on target framework capabilities.
    /// </summary>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Gets the ID value for a specific value.
    /// </summary>
    /// <param name="value">The value to get the ID for.</param>
    /// <returns>The ID assigned to the value.</returns>
    public int GetIdFor(CollectionValueInfoModel<TId> value)
    {
        for (int i = 0; i < ConcreteTypes.Length; i++)
        {
            if (ConcreteTypes[i].Equals(value))
            {
                return i + 1;
            }
        }
        return -1;
    }

    private string _inputHash = string.Empty;

    /// <summary>
    /// Gets a hash string representing the contents of this type info model for incremental generation.
    /// </summary>
    public string InputHash
    {
        get
        {
            if (!string.IsNullOrEmpty(_inputHash)) return _inputHash;
            _inputHash = InputHashCalculator.CalculateHash(this);
            return _inputHash;
        }
    }

    /// <summary>
    /// Writes the contents of this type info model to a TextWriter for hash generation.
    /// </summary>
    /// <param name="writer">The TextWriter to write to.</param>
    /// <exception cref="ArgumentNullException">Thrown when the writer is null.</exception>
#pragma warning disable MA0051 // Sequential property serialization for incremental generator cache hash
    public void WriteToHash(TextWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer), "The TextWriter cannot be null.");
        }

        // Write basic properties
        writer.Write(Namespace);
        writer.Write(ClassName);
        writer.Write(FullTypeName);
        writer.Write(IsGenericType);
        writer.Write(CollectionName);
        writer.Write(CollectionBaseType ?? string.Empty);
        writer.Write(GenerateFactoryMethods);
        writer.Write(GenerateStaticCollection);
        writer.Write(Generic);
        writer.Write(Strategy);
        writer.Write(NameComparison.ToString());
        writer.Write(IncludeReferencedAssemblies);
        writer.Write(UseMethods);
        writer.Write(ReturnType ?? string.Empty);
        writer.Write(ReturnTypeNamespace ?? string.Empty);
        writer.Write(InheritsFromCollectionBase);
        writer.Write(CollectionClassName ?? string.Empty);
        writer.Write(TargetFramework ?? string.Empty);
        writer.Write(IsParentCollection);
        writer.Write(MemberOfParent ?? string.Empty);

        // Write generic type information
        writer.Write(UnboundTypeName);
        writer.Write(DefaultGenericReturnType ?? string.Empty);
        writer.Write(KeyType ?? string.Empty);
        writer.Write(DefaultGenericReturnTypeNamespace ?? string.Empty);

        foreach (var param in TypeParameters.OrderBy(p => p, StringComparer.Ordinal))
        {
            writer.Write(param);
        }

        foreach (var constraint in TypeConstraints.OrderBy(c => c, StringComparer.Ordinal))
        {
            writer.Write(constraint);
        }

        foreach (var ns in RequiredNamespaces.OrderBy(n => n, StringComparer.Ordinal))
        {
            writer.Write(ns);
        }

        // Write lookup properties
        foreach (var lookup in LookupProperties.OrderBy(p => p.PropertyName, StringComparer.Ordinal))
        {
            writer.Write(lookup.PropertyName);
            writer.Write(lookup.PropertyType);
            writer.Write(lookup.StringComparison.ToString());
        }

        // Write concrete types
        foreach (var concreteType in ConcreteTypes.OrderBy(c => c.FullTypeName, StringComparer.Ordinal))
        {
            writer.Write(concreteType.InputHash);
        }
    }
#pragma warning restore MA0051

    /// <summary>
    /// Determines whether the specified <see cref="CollectionTypeInfoModel{TId}"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The CollectionTypeInfoModel to compare with the current instance.</param>
    /// <returns>true if the specified CollectionTypeInfoModel is equal to the current instance; otherwise, false.</returns>
    public bool Equals(CollectionTypeInfoModel<TId>? other)
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
    public override bool Equals(object? obj) => Equals(obj as CollectionTypeInfoModel<TId>);

    /// <summary>
    /// Returns a hash code for this type info model.
    /// </summary>
    /// <returns>A hash code for the current type info model.</returns>
    public override int GetHashCode()
    {
        // Use the InputHash for consistent hash code
        return StringComparer.Ordinal.GetHashCode(InputHash);
    }

    /// <summary>
    /// Returns a string representation of this type info for debugging.
    /// </summary>
    public override string ToString()
    {
        return $@"CollectionTypeInfoModel {{
  Namespace: {Namespace}
  ClassName: {ClassName}
  FullTypeName: {FullTypeName}
  CollectionName: {CollectionName}
  CollectionBaseType: {CollectionBaseType}
  ReturnType: {ReturnType}
  ReturnTypeNamespace: {ReturnTypeNamespace}
  GenerateFactoryMethods: {GenerateFactoryMethods}
  UseMethods: {UseMethods}
  IsGenericType: {IsGenericType}
  InheritsFromCollectionBase: {InheritsFromCollectionBase}
}}";
    }
}