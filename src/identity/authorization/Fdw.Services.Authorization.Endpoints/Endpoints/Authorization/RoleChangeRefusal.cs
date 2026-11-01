using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Turns a refused role change (the grant rule or the last-administrator guard said no) into the HTTP answer.
/// </summary>
internal static class RoleChangeRefusal
{
    /// <summary>
    /// Maps a failed result to the problem response its result code calls for.
    /// </summary>
    /// <param name="refused">The failed result.</param>
    /// <param name="httpContext">The request, for the correlation id.</param>
    /// <returns>The response to send.</returns>
    internal static IResult Of(IGenericResult refused, HttpContext httpContext)
    {
        var (statusCode, problem) = ResultHttpStatusMapper.Map(refused, httpContext);
        return TypedResults.Json(problem, statusCode: statusCode);
    }
}
