using Fdw.Collections;

namespace Fdw.Services.Messaging;

/// <summary>
/// What a UI comment is about (FDW-2142): a label a reviewer puts on it, with an optional colour.
/// </summary>
public interface IUiFeedbackTag : ITypeOption<int, IUiFeedbackTag>, IColorShape
{
    /// <summary>Gets the wording a person sees when choosing the tag.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets whether a comment that carries the tag asks for an answer (a question): any signed-in person may leave a comment whose every tag asks for one, the
    /// people who can answer for the comment's screen are told of it, and they may open it, read its picture and its thread, and answer in it.
    /// </summary>
    bool AsksForAnAnswer { get; }
}
