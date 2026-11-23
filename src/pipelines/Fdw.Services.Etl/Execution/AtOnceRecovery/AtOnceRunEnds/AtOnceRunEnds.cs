using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>
/// What the recovery's one judgment of a half-done run came to (FDW-6986, FDW-7403): the run was ended, it is not an open run carried out at once, or it could not be judged and was left open.
/// A plain collection, not a service type collection: an end has no configuration row.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AtOnceRunEndBase), typeof(IAtOnceRunEnd), typeof(AtOnceRunEnds))]
public abstract partial class AtOnceRunEnds : TypeCollectionBase<AtOnceRunEndBase, IAtOnceRunEnd>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound and one member for each option.
}
