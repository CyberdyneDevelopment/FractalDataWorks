using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Messaging;

/// <summary>
/// Something a person can do to an alert (FDW-2420): acknowledge it, silence it, have it come back in a set time, dismiss it, discard it. A developer adds an
/// action by declaring an option of <see cref="AlertActions"/>, and replaces one by declaring another with the same name.
/// </summary>
public interface IAlertAction : ITypeOption<int, IAlertAction>
{
    /// <summary>Gets the wording a person sees on the menu.</summary>
    string DisplayName { get; }

    /// <summary>Gets what the action does.</summary>
    string Description { get; }

    /// <summary>Gets a value indicating whether the action is taken for everyone the alert is shown to; otherwise it is the person's own.</summary>
    bool ForEveryone { get; }

    /// <summary>Gets the permission the action needs, or <see langword="null"/> when it needs none beyond being allowed to see the alert.</summary>
    string? RequiredPermission { get; }

    /// <summary>Gets a value indicating whether the alert is hidden while the action stands: always, or until the time the chosen length ends.</summary>
    bool HidesTheAlert { get; }

    /// <summary>Gets the lengths of time the action offers, or none when it does not last a chosen time.</summary>
    IReadOnlyList<AlertActionChoice> Choices { get; }
}
