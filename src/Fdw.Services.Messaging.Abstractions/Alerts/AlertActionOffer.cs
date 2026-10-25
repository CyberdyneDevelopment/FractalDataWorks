using System.Collections.Generic;

namespace Fdw.Services.Messaging;

/// <summary>
/// An action an alert allows the caller (FDW-2420): what to show on the menu and whether the caller has already taken it.
/// </summary>
public sealed class AlertActionOffer
{
    /// <summary>Gets or sets the action's name, which a caller sends to take it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the wording a person sees.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the action does.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the action is taken for everyone the alert is shown to.</summary>
    public bool ForEveryone { get; set; }

    /// <summary>Gets or sets a value indicating whether the action is in force now: taken, and for a timed one not yet over.</summary>
    public bool Done { get; set; }

    /// <summary>Gets or sets a value indicating whether a choice of length must be sent with the action.</summary>
    public bool NeedsChoice { get; set; }

    /// <summary>Gets or sets the lengths on offer; empty when the action takes none.</summary>
    public IReadOnlyList<AlertActionChoice> Choices { get; set; } = [];
}
