using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Suppress - suppress escalation for this execution.
/// </summary>
[TypeOption(typeof(EscalationOverrideModes), "Suppress", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SuppressEscalationOverrideMode : EscalationOverrideModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuppressEscalationOverrideMode"/> class.
    /// </summary>
    public SuppressEscalationOverrideMode()
        : base(id: 3, name: "Suppress")
    {
    }
}