using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// TypeCollection for the severity of the notification an escalation level sends. The API serves it to the console, and the escalation level validator checks a level's severity against it (FDW-4693).
/// </summary>
[TypeCollection(typeof(EscalationSeverityBase), typeof(IEscalationSeverity), typeof(EscalationSeverities))]
[ExcludeFromCodeCoverage]
public abstract partial class EscalationSeverities : TypeCollectionBase<EscalationSeverityBase, IEscalationSeverity>
{
}
