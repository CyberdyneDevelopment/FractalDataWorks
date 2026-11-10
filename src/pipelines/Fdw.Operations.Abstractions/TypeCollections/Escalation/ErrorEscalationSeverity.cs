using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Error: An actual failure that requires action
/// </summary>
[TypeOption(typeof(EscalationSeverities), "Error", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ErrorEscalationSeverity : EscalationSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorEscalationSeverity"/> class.
    /// </summary>
    public ErrorEscalationSeverity()
        : base(name: "Error", description: "An actual failure that requires action")
    {
    }
}
