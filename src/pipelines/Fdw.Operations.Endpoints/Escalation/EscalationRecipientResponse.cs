using Fdw.Operations.Abstractions.Escalation;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// DTO for one recipient of an escalation level, with its own type (FDW-4891). It is the request and the response shape alike, and it
/// implements <see cref="IEscalationRecipient"/> so the mapper passes a request's recipients on as they are.
/// </summary>
public class EscalationRecipientResponse : IEscalationRecipient
{
    /// <summary>Gets or sets the recipient: an email address, a webhook URL or an integration key, as its type says.</summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of the recipient (Email, Webhook, IntegrationKey). Required on a request.</summary>
    public string RecipientType { get; set; } = string.Empty;
}
