namespace Fdw.Services.Messaging;

/// <summary>
/// What a UI comment is as an alert (FDW-2420): the name of its kind, and the action whose rows say a person has acknowledged it.
/// </summary>
public static class UiFeedbackAlert
{
    /// <summary>The name of the kind of alert a UI comment is, which every action on its alert is recorded under.</summary>
    public const string Kind = "UiFeedback";

    /// <summary>The name of the action that says a person has acknowledged a comment.</summary>
    public const string AcknowledgeAction = "Acknowledge";
}
