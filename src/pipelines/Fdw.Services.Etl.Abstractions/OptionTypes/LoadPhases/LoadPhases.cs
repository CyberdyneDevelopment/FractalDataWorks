using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// When in a run one load row of a graph destination is written, and what it writes: Load or Reject (FDW-1142).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(LoadPhaseBase), typeof(ILoadPhase), typeof(LoadPhases))]
public abstract partial class LoadPhases : TypeCollectionBase<LoadPhaseBase, ILoadPhase>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
