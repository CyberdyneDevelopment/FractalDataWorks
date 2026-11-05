namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>What opening an alert came to (FDW-3863).</summary>
public enum EscalationOpenOutcome
{
    /// <summary>A new alert was opened and its levels will be walked.</summary>
    Opened,

    /// <summary>The threshold already has an open alert; no second one was opened and the open one is returned.</summary>
    AlreadyOpen,

    /// <summary>The threshold's latest alert was resolved less than the policy's cooldown ago; no alert was opened.</summary>
    CoolingDown,
}
