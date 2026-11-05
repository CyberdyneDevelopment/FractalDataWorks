namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>The states of an escalation alert (FDW-3863), as <c>ops.EscalationAlert.State</c> keeps them.</summary>
public static class EscalationAlertStates
{
    /// <summary>The alert is open and its policy's levels are being walked.</summary>
    public const string Firing = "Firing";

    /// <summary>A person has the alert; no further level is sent.</summary>
    public const string Acknowledged = "Acknowledged";

    /// <summary>The alert is closed; the policy's cooldown counts from the time it was resolved.</summary>
    public const string Resolved = "Resolved";
}
