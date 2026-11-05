using Fdw.Messages;

namespace Fdw.Results;

/// <summary>
/// The named failure a failed result carries when the code that built it gave no message (FDW-3580).
/// </summary>
/// <remarks>
/// Why a named failure and not a throw (Mike, 2026-11-04, ee99604a): a throw from a result factory escapes as a 500 or stops a
/// host phase, which must never throw (ToNewResult was changed from throwing for that reason). The failure still says what
/// went wrong: the code that returned it did not say why, so the defect is that site's, and its code finds it in the logs.
/// </remarks>
public static class UnstatedFailure
{
    /// <summary>The message's code; 9 is the Internal category, an unexpected error in the code itself.</summary>
    public const string Code = "RESULTS-90001";

    /// <summary>Creates the message, one per result, since a message carries its own time and correlation id.</summary>
    /// <returns>The message.</returns>
    public static IGenericMessage Message() => GenericMessage.Create(
        MessageSeverity.Error,
        "A failed result was built without a message: the code that returned it does not say why it failed. Give that failure a MessageLogging message.",
        Code,
        "Fdw.Results");
}
