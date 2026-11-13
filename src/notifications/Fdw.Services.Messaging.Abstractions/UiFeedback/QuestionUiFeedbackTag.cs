using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>Asks someone who knows the screen for information or an answer</summary>
/// <remarks>
/// A comment that carries it is a question (<see cref="UiFeedbackTagBase.AsksForAnAnswer"/>): any signed-in person may leave it, it is also told to the people
/// who can answer for the screen it was asked on, and they may open it and answer it in its thread.
/// </remarks>
[TypeOption(typeof(UiFeedbackTags), "Question")]
public sealed class QuestionUiFeedbackTag : UiFeedbackTagBase
{
    /// <summary>Initializes a new instance of the <see cref="QuestionUiFeedbackTag"/> class.</summary>
    public QuestionUiFeedbackTag() : base("Question", "Question", "Asks someone who knows the screen for information or an answer", "info")
    {
    }

    /// <inheritdoc/>
    public override bool AsksForAnAnswer => true;
}
