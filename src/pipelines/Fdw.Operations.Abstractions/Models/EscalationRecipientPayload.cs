namespace Fdw.Operations.Clients.Models;

/// <summary>
/// One recipient of an escalation level, with its type (FDW-4891).
/// </summary>
public sealed class EscalationRecipientPayload
{
    /// <summary>Gets or sets the recipient: an email address, a webhook URL or an integration key, as its type says.</summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of the recipient (Email, Webhook, IntegrationKey). Required on a create (FDW-3823).</summary>
    public string RecipientType { get; set; } = string.Empty;
}
