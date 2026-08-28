using Microsoft.AspNetCore.Http;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Fdw.Web.RestEndpoints.ErrorMapping;

/// <summary>
/// Maps IGenericResult failures to HTTP status codes and user-safe error responses.
/// Sensitive information (server addresses, SQL text, usernames) is never included in the response —
/// only a generic, category-derived message is returned.
/// </summary>
/// <remarks>
/// Why: HTTP handling is derived from the result code's CATEGORY, not per-code strings. Every
/// categorized ResultCode's number encodes its handling category (number / 10000), and each
/// <see cref="IResultCategory"/> carries the authoritative <see cref="IResultCategory.HttpStatus"/>
/// and <see cref="IResultCategory.IsRetryable"/>. Mapping off the category keeps this correct as
/// codes are added or renumbered — the previous per-code string table silently drifted out of sync
/// the moment a code's Code string changed (e.g. the catalog renumbering), mapping real failures to
/// a generic 500 instead of their intended status.
/// </remarks>
public static class ResultHttpStatusMapper
{
    /// <summary>
    /// Maps an IGenericResult to an HTTP status code and structured ErrorResponse.
    /// </summary>
    /// <param name="result">The result containing failure information.</param>
    /// <param name="httpContext">The HTTP context for correlation ID extraction.</param>
    /// <returns>A tuple of HTTP status code and ErrorResponse.</returns>
    public static (int StatusCode, ProblemDetails Response) Map(IGenericResult result, HttpContext httpContext)
    {
        var referenceId = httpContext.TraceIdentifier;
        var code = ExtractResultCode(result);

        if (code is not null && code.Id >= 10000)
        {
            var category = ResultCategories.ById(code.Id / 10000);
            if (!ReferenceEquals(category, ResultCategories.NotFound) && category.IsFailure)
            {
                return (category.HttpStatus, Build(
                    category.HttpStatus,
                    category.ClientMessage,
                    code.Code,
                    referenceId,
                    category.IsRetryable,
                    category.ClientAction,
                    httpContext));
            }
        }

        // Default: 500 with a generic message (uncategorized/legacy code, or no code at all).
        return (500, Build(
            500,
            "An unexpected error occurred",
            code?.Code ?? "UNKNOWN_ERROR",
            referenceId,
            false,
            "Contact your administrator",
            httpContext));
    }

    private static IResultCode? ExtractResultCode(IGenericResult result)
    {
        // Primary: the result's own code.
        if (result.Code is IResultCode resultCode && !string.IsNullOrEmpty(resultCode.Code))
        {
            return resultCode;
        }

        // Secondary: the first non-empty code in the chain.
        if (result.CodeChain is { Count: > 0 })
        {
            foreach (var chainCode in result.CodeChain)
            {
                if (chainCode is not null && !string.IsNullOrEmpty(chainCode.Code))
                {
                    return chainCode;
                }
            }
        }

        return null;
    }

    /// <summary>Builds the RFC 7807 body, carrying FDW's own fields as extensions.</summary>
    /// <remarks>
    /// Why ProblemDetails and not a bespoke model: every HTTP client, OpenAPI generator and agent
    /// framework already understands status/title/detail. The FDW-specific parts a caller may still
    /// want - the result code, the trace id, whether retrying could help, and what to do about it -
    /// travel as extensions, so nothing is lost and nothing has to be taught.
    /// </remarks>
    private static ProblemDetails Build(
        int status,
        string? message,
        string code,
        string referenceId,
        bool isRetryable,
        string? action,
        HttpContext httpContext)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = message,
            Detail = message,
            Instance = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : null,
        };

        problem.Extensions["code"] = code;
        problem.Extensions["referenceId"] = referenceId;
        problem.Extensions["isRetryable"] = isRetryable;
        problem.Extensions["action"] = action;
        return problem;
    }
}
