using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Messaging;

/// <summary>
/// Base class for the options of <see cref="AlertActions"/>, using the CRTP pattern.
/// </summary>
public abstract class AlertActionBase : TypeOptionBase<AlertActionBase>, IAlertAction
{
    /// <summary>
    /// Initializes the source-generated Empty/NotFound sentinel, which is no action: it is for the person, needs nothing, hides nothing and offers no length of time.
    /// </summary>
    protected AlertActionBase()
        : base(string.Empty)
    {
        Choices = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlertActionBase"/> class.
    /// </summary>
    /// <param name="name">The option name, which a caller sends.</param>
    /// <param name="displayName">The wording a person sees on the menu.</param>
    /// <param name="description">What the action does.</param>
    /// <param name="forEveryone">Whether the action is taken for everyone the alert is shown to, not only the person.</param>
    /// <param name="requiredPermission">The permission the action needs, or <see langword="null"/> for none beyond seeing the alert.</param>
    /// <param name="hidesTheAlert">Whether the alert is hidden while the action stands.</param>
    /// <param name="choices">The lengths of time the action offers; empty when it does not last a chosen time.</param>
    protected AlertActionBase(string name, string displayName, string description, bool forEveryone, string? requiredPermission, bool hidesTheAlert, IReadOnlyList<AlertActionChoice> choices)
        : base(name, displayName, description, "AlertActions")
    {
        ForEveryone = forEveryone;
        RequiredPermission = requiredPermission;
        HidesTheAlert = hidesTheAlert;
        Choices = choices;
    }

    /// <inheritdoc/>
    public bool ForEveryone { get; }

    /// <inheritdoc/>
    public string? RequiredPermission { get; }

    /// <inheritdoc/>
    public bool HidesTheAlert { get; }

    /// <inheritdoc/>
    public IReadOnlyList<AlertActionChoice> Choices { get; }
}
