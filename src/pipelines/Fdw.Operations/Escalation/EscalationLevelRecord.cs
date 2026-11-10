using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Configuration;

namespace Fdw.Operations.Escalation;

/// <summary>
/// Internal adapter mapping <see cref="EscalationLevelConfiguration"/> to <see cref="IEscalationLevel"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class EscalationLevelRecord : IEscalationLevel
{
    private readonly EscalationLevelConfiguration _config;

    internal EscalationLevelRecord(EscalationLevelConfiguration config)
    {
        _config = config;
    }

    public int Level => _config.Level;
    public string Name => _config.Name;
    public int DelayMinutes => _config.DelayMinutes;
    public string NotificationChannel => _config.NotificationChannel;
    public IReadOnlyList<IEscalationRecipient> Recipients => (_config.Recipients ?? []).Select(r => (IEscalationRecipient)new EscalationRecipientRecord(r)).ToList();
    public string? MessageTemplate => _config.Template;
    public string Severity => _config.Severity;
}
