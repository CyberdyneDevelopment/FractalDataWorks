using System;
using System.Collections.Immutable;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for a discovered ServiceTypeCollection.
/// </summary>
internal readonly record struct ServiceTypeCollectionModel(
    string ClassName,
    string Namespace,
    string FullName,
    string BaseTypeName,
    string InterfaceTypeName,
    string MatchKey,
    CollectionKind Kind,
    bool RestrictToCurrentCompilation,
    string? ParentCollectionMatchKey,
    string? ChildName,
    string? ServiceInterfaceTypeName,
    string? ConfigurationInterfaceTypeName,
    string? ConfigurationTypeName,
    string? ProviderTypeName,
    string? ProviderInterfaceTypeName,
    string? ServiceCategory,
    ImmutableArray<ParameterModel> BaseConstructorParameters
)
{
    /// <summary>
    /// Returns true if this collection is a child of another collection.
    /// </summary>
    public bool IsChildCollection => ParentCollectionMatchKey != null;
}