using Fdw.Results;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// What the enable route answers when the connection's state could not be saved (FDW-4193): a write the row-level security policy refuses is the caller's tenant
/// not owning the connection, a 403; any other failure of the save is a fault, a 500. The sentence is ours in both cases and never the database's.
/// </summary>
public static class EnableConnectionAnswers
{
    /// <summary>The HTTP status for a failed save.</summary>
    /// <param name="failure">The failed result.</param>
    /// <returns>403 when the failure is a refusal by policy; otherwise 500.</returns>
    public static int StatusOf(IGenericResult failure)
        => FailureCategory.IsForbidden(failure) ? StatusCodes.Status403Forbidden : StatusCodes.Status500InternalServerError;

    /// <summary>The sentence the caller is told.</summary>
    /// <param name="failure">The failed result.</param>
    /// <returns>Our sentence for a refusal by policy, or for any other failure.</returns>
    public static string SentenceOf(IGenericResult failure)
        => FailureCategory.IsForbidden(failure)
            ? "The connection belongs to another tenant, so its state cannot be changed from this one"
            : "The connection was not enabled";
}
