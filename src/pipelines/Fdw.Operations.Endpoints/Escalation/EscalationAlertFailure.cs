using Fdw.Results;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>Maps the failure of an alerts call to the status the route answers (FDW-3863), by the category its code names and never by its text.</summary>
internal static class EscalationAlertFailure
{
    /// <summary>Gets the HTTP status of a failure.</summary>
    /// <param name="failure">The failed result.</param>
    /// <returns>400 for a validation failure, 404 for one that names something not found, 409 for a state that does not allow the action, and 500 for anything else.</returns>
    public static int StatusOf(IGenericResult failure) =>
        failure.FirstFailureMessage() is { } message
            ? (message.Number / 10000) switch { 2 => 400, 3 => 404, 4 => 409, _ => 500 }
            : 500;
}
