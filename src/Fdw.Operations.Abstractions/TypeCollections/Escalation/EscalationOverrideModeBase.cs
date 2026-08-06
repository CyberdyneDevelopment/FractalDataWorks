using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Base class for escalation override modes using the CRTP pattern.
/// Defines how escalation policy overrides are applied: Default, Replace, Merge, or Suppress.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class EscalationOverrideModeBase : TypeOptionBase<int, EscalationOverrideModeBase>, IEscalationOverrideMode
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected EscalationOverrideModeBase()
        : base(0, "NotFound", "TypeOptions:NotFound", "Not Found", "Unknown escalation override mode", null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationOverrideModeBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this mode.</param>
    /// <param name="name">Name of the mode (must match TypeOption attribute).</param>
    protected EscalationOverrideModeBase(int id, string name)
        : base(id, name, $"TypeOptions:{name}", name, $"Escalation override mode: {name}", null)
    {
    }
}
