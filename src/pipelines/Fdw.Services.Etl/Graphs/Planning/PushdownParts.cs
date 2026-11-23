using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// The component types a store runs when a graph is pushed into it (FDW-6969), one option per <c>ComponentTypes</c> option, by its name.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PushdownPartBase), typeof(IPushdownPart), typeof(PushdownParts))]
public abstract partial class PushdownParts : TypeCollectionBase<PushdownPartBase, IPushdownPart>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
