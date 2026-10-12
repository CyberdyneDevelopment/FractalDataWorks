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
                    httpContext,
                    ClientReason(code, result, category),
                    RelayedDetails(result, code)));
            }
        }

        // Why the messages are read when the result carries no IResultCode: GenericResult.Failure(message)
        // never sets result.Code, so a failure built from a [MessageLogging] message reaches here with
        // only that message's Code ("PREFIX-31550"). Its number encodes the category exactly as a
        // ResultCode's does; ignoring it answered 500 for every such NotFound/validation failure.
        if (code is null && ExtractMessageCode(result) is { } messageCode)
        {
            var category = ResultCategories.ById(messageCode.Number / 10000);
            if (!ReferenceEquals(category, ResultCategories.NotFound) && category.IsFailure)
            {
                return (category.HttpStatus, Build(
                    category.HttpStatus,
                    category.ClientMessage,
                    messageCode.Code,
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

    // Why only a validation refusal whose code opted in: the reason is the caller's own input, named back to them. Every
    // other failure answers with its category's generic text, because its message was written for the log. The details
    // are the result's own, so the code must be the result's own code and not one found further down its chain.
    private static string? ClientReason(IResultCode code, IGenericResult result, IResultCategory category)
        => ReferenceEquals(category, ResultCategories.ByName("Validation")) && ReferenceEquals(result.Code, code)
            ? code.FormatClientMessage(result.Details)
            : null;

    // Why only the result's own code: the details hold what the client captured from the downstream host's answer for THAT
    // failure. A code found further down a chain belongs to a different failure, and its id would trace the wrong call.
    private static IResultDetails? RelayedDetails(IGenericResult result, IResultCode code)
        => ReferenceEquals(result.Code, code) ? result.Details : null;

    private static string? RelayedText(IResultDetails? details, string key)
        => details is not null && details.Data.TryGetValue(key, out var value) && value is string { Length: > 0 } text ? text : null;

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

    /// <summary>
    /// The first message code on <paramref name="result"/> whose number is in a failure category
    /// (20000 and up), parsed from the generator's "PREFIX-NNNNN" form.
    /// </summary>
    /// <remarks>
    /// Success and progress codes (10000-19999) are skipped: a failure that logged progress on its
    /// way must not be answered as a success status.
    /// </remarks>
    private static (string Code, int Number)? ExtractMessageCode(IGenericResult result)
    {
        foreach (var message in result.Messages)
        {
            if (message?.Code is not { Length: > 0 } text)
                continue;

            var dash = text.LastIndexOf('-');
            if (dash < 0 || !int.TryParse(text[(dash + 1)..], System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var number))
                continue;

            if (number >= 20000)
                return (text, number);
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
        HttpContext httpContext,
        string? reason = null,
        IResultDetails? relayed = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = message,
            Detail = string.IsNullOrWhiteSpace(reason) ? message : reason,
            Instance = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : null,
        };

        problem.Extensions["code"] = code;
        AddReferences(problem, referenceId, relayed);
        problem.Extensions["isRetryable"] = isRetryable;
        problem.Extensions["action"] = action;
        return problem;
    }

    // Why the downstream's id leads: a browser sees one failure, so one id must name it. When a downstream host answered with
    // its own referenceId that id is reported as the referenceId, and this host's trace id travels beside it as
    // proxyReferenceId so either log can be found. With nothing relayed, this host's trace id is the only id there is.
    private static void AddReferences(ProblemDetails problem, string referenceId, IResultDetails? relayed)
    {
        if (RelayedText(relayed, ResultDetailKeys.UpstreamReferenceId) is { } upstreamReferenceId)
        {
            problem.Extensions["referenceId"] = upstreamReferenceId;
            problem.Extensions["proxyReferenceId"] = referenceId;
        }
        else
        {
            problem.Extensions["referenceId"] = referenceId;
        }

        if (RelayedText(relayed, ResultDetailKeys.UpstreamCode) is { } upstreamCode)
        {
            problem.Extensions["upstreamCode"] = upstreamCode;
        }
    }
}
