using Fdw.Collections;

namespace Fdw.Services.Messaging;

/// <summary>
/// What a UI comment is about (FDW-2142): a label a reviewer puts on it, with an optional colour.
/// </summary>
public interface IUiFeedbackTag : ITypeOption<int, IUiFeedbackTag>, IColorShape
{
    /// <summary>Gets the wording a person sees when choosing the tag.</summary>
    string DisplayName { get; }
}
