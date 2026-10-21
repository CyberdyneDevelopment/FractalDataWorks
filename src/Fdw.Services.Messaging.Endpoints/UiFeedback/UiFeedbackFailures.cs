using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Answers a failed UI comment operation (FDW-2143) with the status its result code's category carries: a refusal is a 400 that says why, a comment out of reach
/// a 404, one that is another person's a 403, one already in the state asked for a 409.
/// </summary>
internal static class UiFeedbackFailures
{
    /// <summary>
    /// Maps a failure to its response.
    /// </summary>
    /// <param name="failed">The failed result.</param>
    /// <param name="httpContext">The request, whose trace identifier the answer carries.</param>
    /// <returns>The response to send.</returns>
    public static IResult Of(IGenericResult failed, HttpContext httpContext)
    {
        var (statusCode, problem) = ResultHttpStatusMapper.Map(failed, httpContext);
        return TypedResults.Json(problem, statusCode: statusCode);
    }
}
