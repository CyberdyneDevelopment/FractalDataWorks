using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The kinds of key a pipeline run claims: WriteKey, StatedKey, PipelineCap, Alone (FDW-6629) and Exclusive (FDW-6939).</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ClaimKeyTypeBase), typeof(IClaimKeyType), typeof(ClaimKeyTypes))]
public abstract partial class ClaimKeyTypes : TypeCollectionBase<ClaimKeyTypeBase, IClaimKeyType>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
