using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>The consequences a threshold rule can fire (FDW-3936): Alert, Notify, StopRun, DisablePipeline and DisableConnection.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(EscalationTypeBase), typeof(IEscalationType), typeof(EscalationTypes))]
public abstract partial class EscalationTypes : TypeCollectionBase<EscalationTypeBase, IEscalationType>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
