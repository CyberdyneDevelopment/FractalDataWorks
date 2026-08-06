using System.Collections.Immutable;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for a discovered TypeOption.
/// </summary>
internal readonly record struct TypeOptionModel(
    string TypeName,
    string FullTypeName,
    string Namespace,
    string CollectionMatchKey,
    string OptionName,
    int GeneratedId,
    string? Category,
    ImmutableArray<ConstructorModel> Constructors,
    ImmutableArray<LookupPropertyModel> LookupProperties
);