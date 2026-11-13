using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Connections.Http.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>What a paging style builds a read's plan from (FDW-4938): the connection it reads and the additional properties it states.</summary>
/// <param name="ConnectionName">The HTTP connection's name, for the messages. For a read of a container that states its own paging, the connection's name and the container's joined by a slash.</param>
/// <param name="Values">The paging properties the read states, by name, ignoring case.</param>
/// <param name="Logger">The logger refusals are written to.</param>
public sealed record HttpPagingSettings(string ConnectionName, IReadOnlyDictionary<string, string?> Values, ILogger Logger)
{
    /// <summary>
    /// Lays a container's own paging over its connection's, the one place the effective paging of a read is worked out (FDW-5562). The style is the
    /// container's, else the connection's. Each key of that style is the container's value, else the connection's. When the container pages in a style
    /// other than its connection's, the connection's values are for a style of its own, so only the style's <see cref="HttpPagingStyleBase.InheritableProperties"/>
    /// are taken from it: the container states every other key its style needs, or is refused naming it. Nothing is assumed for a key neither states.
    /// </summary>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="connectionStyle">The connection's PagingStyle, or null when it states none.</param>
    /// <param name="connectionValues">The connection's additional properties.</param>
    /// <param name="containerName">The container's name, or null for a read that is not of a container.</param>
    /// <param name="containerStyle">The container's own paging style, or null when it states none.</param>
    /// <param name="containerValues">The paging settings the container states.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>The resolved paging, or the refusal: no style stated at either level, a style no option answers to, or a container key its style does not read.</returns>
    public static IGenericResult<HttpResolvedPaging> For(
        string connectionName,
        string? connectionStyle,
        IReadOnlyDictionary<string, string?> connectionValues,
        string? containerName,
        string? containerStyle,
        IReadOnlyDictionary<string, string?> containerValues,
        ILogger logger)
    {
        var name = string.IsNullOrEmpty(containerName) ? connectionName : string.Concat(connectionName, "/", containerName);
        var containerStates = !string.IsNullOrWhiteSpace(containerStyle);
        var styleSource = containerStates ? HttpPagingSources.Container : HttpPagingSources.Connection;

        var style = HttpPaging.StyleOf(containerStates ? containerStyle : connectionStyle, new HttpPagingSettings(name, connectionValues, logger));
        if (style.IsFailure) return style.ToNewResult<HttpResolvedPaging>();

        var paging = style.Value!;
        var differs = containerStates && !string.Equals(containerStyle!.Trim(), connectionStyle?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var unexpected = paging.UnexpectedProperties(containerValues.Where(entry => !string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key));
        if (unexpected.Count > 0)
            return GenericResult<HttpResolvedPaging>.Failure(HttpPagingLog.PagingPropertyNotExpected(logger, name, paging.Name, unexpected[0]));

        var (values, sources) = Overlay(paging, differs, connectionValues, containerValues);
        return GenericResult<HttpResolvedPaging>.Success(new HttpResolvedPaging(paging, styleSource, new HttpPagingSettings(name, values, logger), sources));
    }

    // Each key the style reads: the container's value, else the connection's when the container may take it (the same style, or a key the style declares inheritable).
    private static (IReadOnlyDictionary<string, string?> Values, IReadOnlyDictionary<string, string> Sources) Overlay(
        HttpPagingStyleBase paging, bool differs, IReadOnlyDictionary<string, string?> connectionValues, IReadOnlyDictionary<string, string?> containerValues)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in paging.ExpectedProperties)
        {
            var inheritable = !differs || paging.InheritableProperties.Contains(key, StringComparer.OrdinalIgnoreCase);
            if (Stated(containerValues, key) is { } own)
            {
                values[key] = own;
                sources[key] = HttpPagingSources.Container;
            }
            else if (inheritable && Stated(connectionValues, key) is { } inherited)
            {
                values[key] = inherited;
                sources[key] = HttpPagingSources.Connection;
            }
        }

        return (values, sources);
    }

    // The value a map states for a key, trimmed; null when it states none or a blank one.
    private static string? Stated(IReadOnlyDictionary<string, string?> values, string key)
    {
        foreach (var entry in values)
        {
            if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(entry.Value))
                return entry.Value.Trim();
        }

        return null;
    }
}
