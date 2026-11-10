using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Warning: A potential issue that needs attention
/// </summary>
[TypeOption(typeof(EscalationSeverities), "Warning", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WarningEscalationSeverity : EscalationSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WarningEscalationSeverity"/> class.
    /// </summary>
    public WarningEscalationSeverity()
        : base(name: "Warning", description: "A potential issue that needs attention")
    {
    }
}
