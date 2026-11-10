using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// IntegrationKey: The recipient is the integration key of an incident service
/// </summary>
[TypeOption(typeof(EscalationRecipientTypes), "IntegrationKey", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IntegrationKeyEscalationRecipientType : EscalationRecipientTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationKeyEscalationRecipientType"/> class.
    /// </summary>
    public IntegrationKeyEscalationRecipientType()
        : base(name: "IntegrationKey", description: "The recipient is the integration key of an incident service")
    {
    }
}
