using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Turns a failed feature read or switch into the problem response its result code calls for (FDW-1580).
/// </summary>
internal static class FeatureProblem
{
    internal static IResult Of(IGenericResult failed, HttpContext httpContext)
    {
        var (statusCode, problem) = ResultHttpStatusMapper.Map(failed, httpContext);
        return TypedResults.Json(problem, statusCode: statusCode);
    }
}
