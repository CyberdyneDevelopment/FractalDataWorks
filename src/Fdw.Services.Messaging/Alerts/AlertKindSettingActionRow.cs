using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// A row of msg.AlertKindSettingAction: one action a tenant chose for a kind of alert, at its place in the order its people see them (FDW-2430).
/// </summary>
[GenerateMapper]
public sealed class AlertKindSettingActionRow
{
    /// <summary>Gets or sets the tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the kind of alert.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the action's name.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the position among the chosen actions, from 0.</summary>
    public int Ordinal { get; set; }
}
