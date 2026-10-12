using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// The moments of a pipeline run a Notify stage can fire at: AfterStage, AfterLoad and OnFailure (FDW-1120).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RunPointBase), typeof(IRunPoint), typeof(RunPoints))]
public abstract partial class RunPoints : TypeCollectionBase<RunPointBase, IRunPoint>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
