namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for a child collection reference (used when generating parent collections).
/// </summary>
internal readonly record struct ChildCollectionModel(
    string ChildName,
    string ChildFullTypeName,
    string ChildClassName
);