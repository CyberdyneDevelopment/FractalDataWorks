using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Base class for escalation severities using the CRTP pattern: Info, Warning, Error or Critical.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class EscalationSeverityBase : TypeOptionBase<EscalationSeverityBase>, IEscalationSeverity
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected EscalationSeverityBase()
        : base(0, "NotFound", "TypeOptions:NotFound", "Not Found", "Unknown escalation severity", null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationSeverityBase"/> class.
    /// </summary>
    /// <param name="name">Name of the option (must match the TypeOption attribute).</param>
    /// <param name="description">What choosing the option means.</param>
    protected EscalationSeverityBase(string name, string description)
        : base(name, $"TypeOptions:{name}", name, description, null)
    {
    }
}
