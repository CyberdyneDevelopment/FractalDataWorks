using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Configuration;

namespace Fdw.Operations.Escalation;

/// <summary>
/// Internal adapter mapping <see cref="EscalationLevelRecipientConfiguration"/> to <see cref="IEscalationRecipient"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class EscalationRecipientRecord : IEscalationRecipient
{
    private readonly EscalationLevelRecipientConfiguration _config;

    internal EscalationRecipientRecord(EscalationLevelRecipientConfiguration config)
    {
        _config = config;
    }

    public string Recipient => _config.Recipient;

    public string RecipientType => _config.RecipientType;
}
