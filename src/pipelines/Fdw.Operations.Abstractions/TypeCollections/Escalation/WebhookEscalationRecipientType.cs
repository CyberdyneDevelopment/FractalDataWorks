using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Webhook: The recipient is a webhook URL
/// </summary>
[TypeOption(typeof(EscalationRecipientTypes), "Webhook", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WebhookEscalationRecipientType : EscalationRecipientTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookEscalationRecipientType"/> class.
    /// </summary>
    public WebhookEscalationRecipientType()
        : base(name: "Webhook", description: "The recipient is a webhook URL")
    {
    }
}
