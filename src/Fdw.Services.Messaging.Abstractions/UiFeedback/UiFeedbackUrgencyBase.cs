using Fdw.Collections;

namespace Fdw.Services.Messaging;

/// <summary>
/// Base class for the options of <see cref="UiFeedbackUrgencies"/>, using the CRTP pattern.
/// </summary>
public abstract class UiFeedbackUrgencyBase : TypeOptionBase<UiFeedbackUrgencyBase>, IUiFeedbackUrgency
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UiFeedbackUrgencyBase"/> class.
    /// </summary>
    /// <param name="name">The option name, which a comment states.</param>
    /// <param name="displayName">The name shown to a person choosing the urgency.</param>
    /// <param name="description">What the urgency means.</param>
    /// <param name="color">The colour a console draws it in: neutral, info, success, warning or error.</param>
    /// <param name="rank">How urgent it is; higher is more urgent.</param>
    /// <param name="needsAcknowledgement">Whether an alert about a comment of this urgency stays until a person acknowledges it.</param>
    /// <param name="severity">The severity of the message that tells a reader about a comment of this urgency.</param>
    protected UiFeedbackUrgencyBase(string name, string displayName, string description, string color, int rank, bool needsAcknowledgement, string severity)
        : base(name, displayName, description, "UiFeedbackUrgencies")
    {
        Color = color;
        Rank = rank;
        NeedsAcknowledgement = needsAcknowledgement;
        Severity = severity;
    }

    /// <inheritdoc/>
    public string Color { get; }

    /// <inheritdoc/>
    public int Rank { get; }

    /// <inheritdoc/>
    public bool NeedsAcknowledgement { get; }

    /// <inheritdoc/>
    public string Severity { get; }
}
