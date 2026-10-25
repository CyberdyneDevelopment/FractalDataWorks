namespace Fdw.Services.Messaging;

/// <summary>
/// What a platform message is as an alert (FDW-2429): the name of its kind, and the action whose rows say a person has dismissed it.
/// </summary>
public static class MessageAlert
{
    /// <summary>The name of the kind of alert a message is, which every action on its alert is recorded under.</summary>
    public const string Kind = "Message";

    /// <summary>The name of the action that says a person has dismissed a message.</summary>
    public const string DismissAction = "Dismiss";
}
