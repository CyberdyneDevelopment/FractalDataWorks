using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// TypeCollection for escalation override modes that control how policy overrides are applied.
/// </summary>
/// <remarks>
/// <para>
/// Override modes define how escalation policies are modified:
/// <list type="bullet">
/// <item><description>Default - Use default policy without modifications</description></item>
/// <item><description>Replace - Replace entire policy with override</description></item>
/// <item><description>Merge - Add override recipients to existing policy</description></item>
/// <item><description>Suppress - Suppress escalation for this execution</description></item>
/// </list>
/// </para>
/// </remarks>
[TypeCollection(typeof(EscalationOverrideModeBase), typeof(IEscalationOverrideMode), typeof(EscalationOverrideModes))]
[ExcludeFromCodeCoverage]
public abstract partial class EscalationOverrideModes : TypeCollectionBase<EscalationOverrideModeBase, IEscalationOverrideMode>
{
}

// =============================================================================
// Escalation Override Mode Options
// =============================================================================