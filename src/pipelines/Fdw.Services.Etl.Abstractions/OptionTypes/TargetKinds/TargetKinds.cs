using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What a source reads or a destination writes (FDW-3089, FDW-3193): the declared container, the container of a data set, one file of it, a list of
/// files read as one stream, or files split by a field. A kind says which settings of the component it needs and which it refuses, for a source and
/// for a destination, so a component is judged by the kind it states and the console draws its form from the kinds.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TargetKindBase), typeof(ITargetKind), typeof(TargetKinds))]
public abstract partial class TargetKinds : TypeCollectionBase<TargetKindBase, ITargetKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
