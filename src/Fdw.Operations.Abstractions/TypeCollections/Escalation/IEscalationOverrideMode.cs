using Fdw.Collections;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Interface for escalation override modes that control how overrides are applied to policies.
/// </summary>
public interface IEscalationOverrideMode : ITypeOption<int, EscalationOverrideModeBase>
{
}
