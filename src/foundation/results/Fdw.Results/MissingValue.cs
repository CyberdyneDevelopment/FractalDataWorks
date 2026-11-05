using Fdw.Messages;

namespace Fdw.Results;

/// <summary>
/// The named failure for a result that succeeded without the value its reader needs (FDW-3580).
/// </summary>
/// <remarks>
/// Why named: a reader guarding <c>!result.IsSuccess || result.Value is null</c> used to report <c>CurrentMessage ?? "..."</c>,
/// a default message for the success-without-value half. That half is the producer's defect, and this message says so with a
/// code the logs can find, as <see cref="UnstatedFailure"/> does for a failure without a message (Mike, ee99604a).
/// </remarks>
public static class MissingValue
{
    /// <summary>The message's code; 9 is the Internal category, an unexpected error in the code itself.</summary>
    public const string Code = "RESULTS-90002";

    /// <summary>Creates the message, one per read, since a message carries its own time and correlation id.</summary>
    /// <param name="valueType">The name of the value's type the result was declared to carry.</param>
    /// <returns>The message.</returns>
    public static IGenericMessage Message(string valueType) => GenericMessage.Create(
        MessageSeverity.Error,
        $"A result succeeded without its {valueType} value: the code that returned it reported success with nothing to return.",
        Code,
        "Fdw.Results");
}
