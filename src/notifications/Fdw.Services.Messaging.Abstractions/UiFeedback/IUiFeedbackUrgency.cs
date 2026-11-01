using Fdw.Collections;

namespace Fdw.Services.Messaging;

/// <summary>
/// How urgent a UI comment is (FDW-2142): a colour, a rank and whether an alert about it must be acknowledged.
/// </summary>
public interface IUiFeedbackUrgency : ITypeOption<int, IUiFeedbackUrgency>, IColorShape, IRankShape, IAcknowledgementShape
{
    /// <summary>Gets the wording a person sees when choosing the urgency.</summary>
    string DisplayName { get; }

    /// <summary>Gets the severity of the message that tells a reader about a comment of this urgency, such as <c>Info</c> or <c>Error</c>.</summary>
    string Severity { get; }
}
