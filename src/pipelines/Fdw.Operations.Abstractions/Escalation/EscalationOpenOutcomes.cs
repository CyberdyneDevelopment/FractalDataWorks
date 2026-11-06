using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>What opening an alert can come to (FDW-4150, was the EscalationOpenOutcome enum): opened, already open, or cooling down.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(EscalationOpenOutcomeBase), typeof(IEscalationOpenOutcome), typeof(EscalationOpenOutcomes))]
public abstract partial class EscalationOpenOutcomes : TypeCollectionBase<EscalationOpenOutcomeBase, IEscalationOpenOutcome>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
