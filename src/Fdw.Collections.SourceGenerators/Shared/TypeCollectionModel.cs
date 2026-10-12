using System.Collections.Immutable;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for a discovered TypeCollection.
/// </summary>
/// <remarks>
/// <c>SentinelHoldsZeroId</c> is true when the collection's base exposes an overridable int <c>Id</c>. The
/// generated NotFound then states <c>Id => 0</c> itself: options derive their ids from their type names,
/// so no base constructor <c>id</c> parameter is left to default it to 0.
/// </remarks>
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
    ImmutableArray<ParameterModel> BaseConstructorParameters,
    bool SentinelHoldsZeroId
)
{
    /// <summary>
    /// Returns true if this collection is a child of another collection.
    /// </summary>
    public bool IsChildCollection => ParentCollectionMatchKey != null;
}