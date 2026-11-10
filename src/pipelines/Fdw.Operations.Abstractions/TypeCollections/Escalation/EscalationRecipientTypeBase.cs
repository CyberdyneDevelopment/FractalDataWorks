using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Base class for escalation recipient types using the CRTP pattern: Email, Webhook or IntegrationKey.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class EscalationRecipientTypeBase : TypeOptionBase<EscalationRecipientTypeBase>, IEscalationRecipientType
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected EscalationRecipientTypeBase()
        : base(0, "NotFound", "TypeOptions:NotFound", "Not Found", "Unknown escalation recipient type", null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationRecipientTypeBase"/> class.
    /// </summary>
    /// <param name="name">Name of the option (must match the TypeOption attribute).</param>
    /// <param name="description">What choosing the option means.</param>
    protected EscalationRecipientTypeBase(string name, string description)
        : base(name, $"TypeOptions:{name}", name, description, null)
    {
    }
}
