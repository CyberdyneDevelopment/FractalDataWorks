using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Logging;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Works out the paging a read of a container follows (FDW-5562): the container's own style and settings, which reach its runtime metadata
    /// (<see cref="ContainerPagingMetadata"/>), laid over the connection's by <see cref="HttpPagingSettings.For"/>, the one place that is done.
    /// </summary>
    /// <param name="connection">The HTTP connection the container belongs to.</param>
    /// <param name="container">The container read, or null for a read that is not of a container.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>The resolved paging, or the refusal: no style at either level, a style no option answers to, or a container setting its style does not read.</returns>
    public static IGenericResult<HttpResolvedPaging> ResolveFor(HttpConnectionConfigurationBase connection, IStorageContainer? container, ILogger logger)
    {
        var (style, settings) = PagingOf(container);
        return HttpPagingSettings.For(
            connection.Name, connection.PagingStyle, new Dictionary<string, string?>(connection.AdditionalProperties, StringComparer.OrdinalIgnoreCase),
            container?.Name, style, settings, logger);
    }

    // The style and the settings a container states for its own paging, read from its runtime metadata; none for a read that is not of a container.
    private static (string? Style, IReadOnlyDictionary<string, string?> Settings) PagingOf(IStorageContainer? container)
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? style = null;
        if (container?.Metadata is not { } metadata)
            return (style, settings);

        foreach (var entry in metadata)
        {
            if (string.Equals(entry.Key, ContainerPagingMetadata.Style, StringComparison.Ordinal))
                style = entry.Value as string;
            else if (entry.Key.StartsWith(ContainerPagingMetadata.SettingPrefix, StringComparison.Ordinal))
                settings[entry.Key[ContainerPagingMetadata.SettingPrefix.Length..]] = entry.Value as string;
        }

        return (style, settings);
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
