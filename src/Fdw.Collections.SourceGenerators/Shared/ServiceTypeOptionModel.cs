using System;
using System.Collections.Immutable;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for a discovered ServiceTypeOption.
/// </summary>
internal readonly record struct ServiceTypeOptionModel(
    string TypeName,
    string FullTypeName,
    string Namespace,
    string CollectionMatchKey,
    string OptionName,
    Guid GeneratedId,
    ImmutableArray<ConstructorModel> Constructors,
    ImmutableArray<LookupPropertyModel> LookupProperties
);