namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>A consequence a rule can fire (FDW-3948), with what a rule of it must state.</summary>
public sealed class EscalationTypeResponse
{
    /// <summary>Gets or sets the consequence's name, which a rule states as its escalation.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the consequence does.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether a rule with this consequence must name an escalation policy.</summary>
    public bool NeedsPolicy { get; set; }

    /// <summary>Gets or sets a value indicating whether a rule with this consequence must name notification rules.</summary>
    public bool NeedsNotificationRules { get; set; }

    /// <summary>Gets or sets a value indicating whether the consequence acts on the run the values were measured in.</summary>
    public bool ActsOnRun { get; set; }
}
