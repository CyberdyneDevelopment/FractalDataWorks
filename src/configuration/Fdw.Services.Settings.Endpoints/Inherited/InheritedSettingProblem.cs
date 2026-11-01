using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Turns a failed settings read or change into the problem response its result code calls for (FDW-1579).
/// </summary>
internal static class InheritedSettingProblem
{
    internal static IResult Of(IGenericResult failed, HttpContext httpContext)
    {
        var (statusCode, problem) = ResultHttpStatusMapper.Map(failed, httpContext);
        return TypedResults.Json(problem, statusCode: statusCode);
    }
}
