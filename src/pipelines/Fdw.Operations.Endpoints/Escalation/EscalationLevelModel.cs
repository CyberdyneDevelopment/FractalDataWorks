using System.Collections.Generic;
using Fdw.Operations.Abstractions.Escalation;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Concrete implementation of <see cref="IEscalationLevel"/> for use in endpoint request/response mapping.
/// </summary>
public class EscalationLevelModel : IEscalationLevel
{
    /// <inheritdoc/>
    public int Level { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public int DelayMinutes { get; set; }

    /// <inheritdoc/>
    public string NotificationChannel { get; set; } = string.Empty;

    /// <inheritdoc/>
    public IReadOnlyList<IEscalationRecipient> Recipients { get; set; } = [];

    /// <inheritdoc/>
    public string? MessageTemplate { get; set; }

    /// <inheritdoc />
    public string Severity { get; set; } = string.Empty;
}
