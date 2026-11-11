using System;
using Fdw.Results;
using Fdw.Services.Connections.Http.Logging;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>Finds the paging style a connection names and builds a read's plan from it (FDW-4938), the one place a missing style is refused.</summary>
public static class HttpPaging
{
    /// <summary>Resolves the style a connection names.</summary>
    /// <param name="style">The connection's PagingStyle.</param>
    /// <param name="settings">The connection's paging settings, for the messages.</param>
    /// <returns>The style; or the refusal when the connection names none, or one no option answers to.</returns>
    public static IGenericResult<HttpPagingStyleBase> StyleOf(string? style, HttpPagingSettings settings)
    {
        if (string.IsNullOrWhiteSpace(style))
            return GenericResult<HttpPagingStyleBase>.Failure(HttpPagingLog.PagingStyleNotStated(settings.Logger, settings.ConnectionName));

        var found = HttpPagingStyles.ByName(style.Trim());
        return found.IsEmpty
            ? GenericResult<HttpPagingStyleBase>.Failure(HttpPagingLog.PagingStyleUnknown(settings.Logger, settings.ConnectionName, style))
            : GenericResult<HttpPagingStyleBase>.Success(found);
    }

    /// <summary>Builds one read's plan in the style a connection names.</summary>
    /// <param name="style">The connection's PagingStyle.</param>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="window">The rows the read returns.</param>
    /// <returns>The plan, or the refusal naming what is missing.</returns>
    public static IGenericResult<HttpPagingPlan> PlanFor(string? style, HttpPagingSettings settings, HttpReadWindow window)
    {
        var resolved = StyleOf(style, settings);
        return resolved.IsFailure ? resolved.ToNewResult<HttpPagingPlan>() : resolved.Value!.Plan(settings, window);
    }

    /// <summary>Writes a query's window into one request in the style a connection names.</summary>
    /// <param name="style">The connection's PagingStyle.</param>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="skip">The rows the query skips.</param>
    /// <param name="take">The most rows the query returns; null for all.</param>
    /// <returns>How the request asks for the window, or the refusal.</returns>
    public static IGenericResult<HttpPagingWindow> WindowFor(string? style, HttpPagingSettings settings, int skip, int? take)
    {
        var resolved = StyleOf(style, settings);
        return resolved.IsFailure ? resolved.ToNewResult<HttpPagingWindow>() : resolved.Value!.Window(settings, skip, take);
    }
}
