using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The kinds of thing a pipeline writes whose container a claim can name: a <c>Container</c> and a <c>DataSet</c>.
/// </summary>
/// <remarks>A write is found by its kind's name through <c>ByName</c>; a kind that matches none (a data store, a pipeline) gets <c>NotFound</c>, and the claim names no container for it.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ClaimUseKindBase), typeof(IClaimUseKind), typeof(ClaimUseKinds))]
public abstract partial class ClaimUseKinds : TypeCollectionBase<ClaimUseKindBase, IClaimUseKind>
{
}
