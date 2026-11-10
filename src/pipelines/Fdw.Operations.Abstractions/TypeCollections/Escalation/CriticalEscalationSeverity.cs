using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Critical: A severe failure that needs an immediate response
/// </summary>
[TypeOption(typeof(EscalationSeverities), "Critical", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CriticalEscalationSeverity : EscalationSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CriticalEscalationSeverity"/> class.
    /// </summary>
    public CriticalEscalationSeverity()
        : base(name: "Critical", description: "A severe failure that needs an immediate response")
    {
    }
}
