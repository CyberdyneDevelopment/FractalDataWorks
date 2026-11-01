using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// A row of msg.AlertEveryoneAction: one action taken on one alert for everyone it is shown to, such as a discard (FDW-2420).
/// </summary>
[GenerateMapper]
public sealed class AlertEveryoneActionRow
{
    /// <summary>Gets or sets the tenant the alert belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the kind of alert.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the alert's identifier in its kind.</summary>
    public Guid AlertId { get; set; }

    /// <summary>Gets or sets the action's name.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the person who took the action.</summary>
    public Guid ActedByUserId { get; set; }

    /// <summary>Gets or sets the person's name as it was when they acted.</summary>
    public string ActedByName { get; set; } = string.Empty;

    /// <summary>Gets or sets when they acted.</summary>
    public DateTimeOffset ActedAt { get; set; }

    /// <summary>Gets or sets when the action ends, or <see langword="null"/> when it does not.</summary>
    public DateTimeOffset? Until { get; set; }
}
