using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// A row of msg.AlertKindSetting: a tenant chose the actions a kind of alert offers its people (FDW-2430). The chosen actions are msg.AlertKindSettingAction rows.
/// </summary>
[GenerateMapper]
public sealed class AlertKindSettingRow
{
    /// <summary>Gets or sets the tenant, the company whose people the choice is for.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the kind of alert.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the person who made the choice.</summary>
    public Guid ChangedByUserId { get; set; }

    /// <summary>Gets or sets the person's name as it was when they made it.</summary>
    public string ChangedByName { get; set; } = string.Empty;

    /// <summary>Gets or sets when the choice was made.</summary>
    public DateTimeOffset ChangedAt { get; set; }
}
