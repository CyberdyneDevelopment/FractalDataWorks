using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Info: Informational, low priority
/// </summary>
[TypeOption(typeof(EscalationSeverities), "Info", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InfoEscalationSeverity : EscalationSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InfoEscalationSeverity"/> class.
    /// </summary>
    public InfoEscalationSeverity()
        : base(name: "Info", description: "Informational, low priority")
    {
    }
}
