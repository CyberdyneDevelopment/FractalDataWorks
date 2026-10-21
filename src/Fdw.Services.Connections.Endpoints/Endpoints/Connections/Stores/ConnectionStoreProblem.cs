using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Turns a failed grant or assignment into the problem response its result code calls for (FDW-1581).
/// </summary>
internal static class ConnectionStoreProblem
{
    internal static IResult Of(IGenericResult failed, HttpContext httpContext)
    {
        var (statusCode, problem) = ResultHttpStatusMapper.Map(failed, httpContext);
        return TypedResults.Json(problem, statusCode: statusCode);
    }
}
