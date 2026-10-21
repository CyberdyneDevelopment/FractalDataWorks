using Fdw.Collections;

namespace Fdw.Services.Messaging;

/// <summary>
/// Base class for the options of <see cref="UiFeedbackTags"/>, using the CRTP pattern.
/// </summary>
public abstract class UiFeedbackTagBase : TypeOptionBase<UiFeedbackTagBase>, IUiFeedbackTag
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UiFeedbackTagBase"/> class.
    /// </summary>
    /// <param name="name">The option name, which a comment states.</param>
    /// <param name="displayName">The name shown to a person choosing the tag.</param>
    /// <param name="description">What the tag means.</param>
    /// <param name="color">The colour a console draws it in: neutral, info, success, warning or error.</param>
    protected UiFeedbackTagBase(string name, string displayName, string description, string color)
        : base(name, displayName, description, "UiFeedbackTags")
    {
        Color = color;
    }

    /// <inheritdoc/>
    public string Color { get; }
}
