namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>The answer to opening an alert (FDW-3863).</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Alert">The alert that was opened, or the one already open; none when the threshold is cooling down.</param>
public sealed record EscalationOpened(EscalationOpenOutcome Outcome, EscalationAlert? Alert);
