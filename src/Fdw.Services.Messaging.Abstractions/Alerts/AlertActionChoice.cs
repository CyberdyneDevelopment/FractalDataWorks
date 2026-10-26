namespace Fdw.Services.Messaging;

/// <summary>
/// One of the lengths of time a timed alert action offers, such as snoozing for an hour (FDW-2420). The server turns the choice into a time; a caller never sends one.
/// </summary>
public sealed class AlertActionChoice
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlertActionChoice"/> class.
    /// </summary>
    /// <param name="name">The name a caller sends to pick this length.</param>
    /// <param name="displayName">The wording a person sees.</param>
    /// <param name="minutes">How long the action lasts from the moment it is taken, in minutes.</param>
    public AlertActionChoice(string name, string displayName, int minutes)
    {
        Name = name;
        DisplayName = displayName;
        Minutes = minutes;
    }

    /// <summary>Gets the name a caller sends to pick this length.</summary>
    public string Name { get; }

    /// <summary>Gets the wording a person sees.</summary>
    public string DisplayName { get; }

    /// <summary>Gets how long the action lasts from the moment it is taken, in minutes.</summary>
    public int Minutes { get; }
}
