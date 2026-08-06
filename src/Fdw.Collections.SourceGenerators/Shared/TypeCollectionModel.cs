using System.Collections.Immutable;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for a discovered TypeCollection.
/// </summary>
internal readonly record struct TypeCollectionModel(
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
    ImmutableArray<ParameterModel> BaseConstructorParameters
)
{
    /// <summary>
    /// Returns true if this collection is a child of another collection.
    /// </summary>
    public bool IsChildCollection => ParentCollectionMatchKey != null;
}