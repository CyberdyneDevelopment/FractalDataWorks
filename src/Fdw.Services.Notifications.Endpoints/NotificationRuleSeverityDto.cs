namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// A severity a notification rule may carry, with the priority a notification sent through the rule has (FDW-2931).
/// </summary>
public sealed class NotificationRuleSeverityDto
{
    /// <summary>Gets or sets the severity as a rule states it.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the priority a notification sent through a rule of this severity has.</summary>
    public required string Priority { get; set; }
}
