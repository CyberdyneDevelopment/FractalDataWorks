using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// TypeCollection for the type of an escalation level's recipients. The API serves it to the console, and the recipient validator checks a recipient's type against it (FDW-4693).
/// </summary>
[TypeCollection(typeof(EscalationRecipientTypeBase), typeof(IEscalationRecipientType), typeof(EscalationRecipientTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class EscalationRecipientTypes : TypeCollectionBase<EscalationRecipientTypeBase, IEscalationRecipientType>
{
}
