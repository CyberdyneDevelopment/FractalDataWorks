using FastEndpoints;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Turns a failed result into the FastEndpoints problem the map endpoints answer with.</summary>
/// <remarks>
/// The status and the user-safe message still come from <see cref="ResultHttpStatusMapper"/>, which derives them from the
/// result code's category, so the map says exactly what any other endpoint would. Only the body's type differs. The
/// result code and the trace id are carried in the problem. Whether the failure is retryable and the client action the
/// mapper suggests have no place in this type and are not sent.
/// </remarks>
internal static class MapProblemDetails
{
    /// <summary>Builds the problem for a failed result.</summary>
    /// <param name="result">The failed result.</param>
    /// <param name="httpContext">The request, for its path and trace id.</param>
    public static ProblemDetails From(IGenericResult result, HttpContext httpContext)
    {
        var (statusCode, mapped) = ResultHttpStatusMapper.Map(result, httpContext);
        var code = (string)mapped.Extensions["code"]!;
        return new ProblemDetails
        {
            // Type and Title are read-only on this type: FastEndpoints derives them from the status.
            Status = statusCode,
            Detail = mapped.Detail!,
            Instance = mapped.Instance!,
            TraceId = httpContext.TraceIdentifier,
            Errors = [new ProblemDetails.Error { Name = "result", Reason = mapped.Detail!, Code = code }],
        };
    }
}
