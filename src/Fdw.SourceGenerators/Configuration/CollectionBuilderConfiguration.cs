using System;

namespace Fdw.SourceGenerators.Configuration;

/// <summary>
/// Configuration for the GenericCollectionBuilder to support different collection types
/// (TypeCollection, ServiceTypeCollection, etc.) with different base classes and arities.
/// </summary>
public sealed class CollectionBuilderConfiguration
{
    /// <summary>
    /// Gets the fully qualified name of the base collection class (e.g., "Fdw.Collections.TypeCollectionBase").
    /// </summary>
    public string BaseCollectionTypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the generic arity of the base collection type (e.g., 1 for TypeCollectionBase&lt;T&gt;, 5 for ServiceTypeCollectionBase&lt;T,T1,T2,T3,T4&gt;).
    /// </summary>
    public int BaseCollectionArity { get; init; }

    /// <summary>
    /// Gets the namespace containing the base collection types (e.g., "Fdw.Collections").
    /// </summary>
    public string BaseNamespace { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the attribute that marks collection types (e.g., "TypeCollection", "ServiceTypeCollection").
    /// </summary>
    public string CollectionAttributeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the attribute that marks collection value/option types (e.g., "TypeOption", "ServiceTypeOption").
    /// </summary>
    public string ValueAttributeName { get; init; } = string.Empty;

    /// <summary>
    /// Creates a configuration for TypeCollections (Fdw.Collections).
    /// </summary>
    public static CollectionBuilderConfiguration ForTypeCollections() => new()
    {
        BaseCollectionTypeName = "Fdw.Collections.TypeCollectionBase",
        BaseCollectionArity = 1,
        BaseNamespace = "Fdw.Collections",
        CollectionAttributeName = "TypeCollection",
        ValueAttributeName = "TypeOption"
    };

    /// <summary>
    /// Creates a configuration for ServiceTypeCollections (Fdw.ServiceTypes).
    /// </summary>
    public static CollectionBuilderConfiguration ForServiceTypeCollections() => new()
    {
        BaseCollectionTypeName = "Fdw.ServiceTypes.ServiceTypeCollectionBase",
        BaseCollectionArity = 5,
        BaseNamespace = "Fdw.ServiceTypes",
        CollectionAttributeName = "ServiceTypeCollection",
        ValueAttributeName = "ServiceTypeOption"
    };

    /// <summary>
    /// Validates that all required configuration values are set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseCollectionTypeName))
            throw new InvalidOperationException("BaseCollectionTypeName must be set.");

        if (BaseCollectionArity <= 0)
            throw new InvalidOperationException("BaseCollectionArity must be greater than zero.");

        if (string.IsNullOrWhiteSpace(BaseNamespace))
            throw new InvalidOperationException("BaseNamespace must be set.");

        if (string.IsNullOrWhiteSpace(CollectionAttributeName))
            throw new InvalidOperationException("CollectionAttributeName must be set.");

        if (string.IsNullOrWhiteSpace(ValueAttributeName))
            throw new InvalidOperationException("ValueAttributeName must be set.");
    }
}
