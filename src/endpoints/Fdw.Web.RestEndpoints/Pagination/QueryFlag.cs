using Fdw.Results;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>
/// Reads a request's true-or-false query parameter, such as <c>?total=true</c> or <c>?paged=true</c> (FDW-1194).
/// </summary>
/// <remarks>
/// Absent means what the caller states -- false for an opt-in such as <c>paged</c>, true for an opt-out such as
/// <c>containers</c> -- which keeps a request that does not mention the parameter answered as it always was. A value
/// that is neither <c>true</c> nor <c>false</c> is refused, never read as the default.
/// </remarks>
public static class QueryFlag
{
    /// <summary>Reads the flag named <paramref name="name"/>.</summary>
    /// <param name="context">The current request.</param>
    /// <param name="name">The query parameter.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <param name="whenAbsent">What the flag is when the request does not mention it: false for an opt-in, true for an opt-out.</param>
    /// <returns>Whether the flag is set; a failure when the value is neither true nor false.</returns>
    public static IGenericResult<bool> Read(HttpContext context, string name, ILogger logger, bool whenAbsent = false)
    {
        if (!context.Request.Query.TryGetValue(name, out var raw))
            return GenericResult<bool>.Success(whenAbsent);

        return bool.TryParse(raw.ToString(), out var value)
            ? GenericResult<bool>.Success(value)
            : GenericResult<bool>.Failure(EndpointLogger.PaginationValueInvalid(logger, name, raw.ToString(), "true or false"));
    }
}
