using Fdw.Collections;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Interface for the severity of the notification an escalation level sends.
/// </summary>
public interface IEscalationSeverity : ITypeOption<int, EscalationSeverityBase>
{
}
