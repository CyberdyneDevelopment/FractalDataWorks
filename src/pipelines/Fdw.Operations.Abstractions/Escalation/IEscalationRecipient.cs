namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>
/// One recipient of an escalation level, with the type that says what its value is (FDW-4891).
/// </summary>
/// <remarks>
/// Why the type is the recipient's and not the level's: the type is stored on each recipient row, so a level can hold recipients of
/// different types and a level on a channel that needs none holds no recipient, and so no type.
/// </remarks>
public interface IEscalationRecipient
{
    /// <summary>
    /// Gets the recipient: an email address, a webhook URL or an integration key, as its <see cref="RecipientType"/> says.
    /// </summary>
    string Recipient { get; }

    /// <summary>
    /// Gets the type of this recipient (Email, Webhook, IntegrationKey). The caller states it; there is no default (FDW-3823).
    /// </summary>
    string RecipientType { get; }
}
