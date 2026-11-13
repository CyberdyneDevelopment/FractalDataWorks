using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>
/// How a container of an HTTP data store pages its rows (FDW-5562): its own style and settings laid over its connection's, checked by the paging style's own
/// code, the same code a read of the container follows.
/// </summary>
public sealed class HttpContainerPagingRules : IContainerPagingRules
{
    /// <summary>Gets the one instance: the rules hold no state.</summary>
    public static HttpContainerPagingRules Instance { get; } = new();

    /// <inheritdoc/>
    /// <remarks>
    /// A container that states no paging has nothing to check, whatever its connection states: a connection saved before paging was stated is still read as
    /// before by everything that does not page.
    /// </remarks>
    public IGenericResult Check(
        IConnectionImplementationConfiguration connection, string containerName, string? style, IReadOnlyDictionary<string, string?> settings, ILogger logger)
    {
        if (!States(style, settings))
            return GenericResult.Success();

        // A container's settings are for the style it names, so they are never stated without one (FDW-5562): its own override is its style, and the
        // settings are what it changes within it.
        if (string.IsNullOrWhiteSpace(style))
            return GenericResult.Failure(HttpPagingLog.PagingSettingsWithoutStyle(logger, string.Concat(connection.Name, "/", containerName)));

        var resolved = Resolved(connection, containerName, style, settings, logger);
        return resolved.IsFailure || resolved.Value is null ? resolved : resolved.Value.Style.Check(resolved.Value.Settings);
    }

    /// <inheritdoc/>
    public IGenericResult<ContainerPagingView?> Resolve(
        IConnectionImplementationConfiguration connection, string containerName, string? style, IReadOnlyDictionary<string, string?> settings, ILogger logger)
    {
        if (connection is HttpConnectionConfigurationBase http && !States(style, settings) && string.IsNullOrWhiteSpace(http.PagingStyle))
            return GenericResult<ContainerPagingView?>.Success(null);

        var resolved = Resolved(connection, containerName, style, settings, logger);
        if (resolved.IsFailure || resolved.Value is null)
            return resolved.ToNewResult<ContainerPagingView?>();

        var paging = resolved.Value;
        var values = new List<ContainerPagingValue>();
        foreach (var key in paging.Style.ExpectedProperties)
        {
            if (paging.Settings.Values.TryGetValue(key, out var stated) && stated is not null)
                values.Add(new ContainerPagingValue(key, stated, paging.Sources[key]));
            else if (ServiceTypeDefaults.TryResolve(paging.Style, key, out var declared) && declared is int number)
                values.Add(new ContainerPagingValue(key, number.ToString(CultureInfo.InvariantCulture), HttpPagingSources.Style));
        }

        return GenericResult<ContainerPagingView?>.Success(new ContainerPagingView(paging.Style.Name, paging.StyleSource, values));
    }

    // Whether the container states any paging of its own: a style, or a setting with a value.
    private static bool States(string? style, IReadOnlyDictionary<string, string?> settings)
        => !string.IsNullOrWhiteSpace(style) || settings.Values.Any(value => !string.IsNullOrWhiteSpace(value));

    private static IGenericResult<HttpResolvedPaging> Resolved(
        IConnectionImplementationConfiguration connection, string containerName, string? style, IReadOnlyDictionary<string, string?> settings, ILogger logger)
    {
        if (connection is not HttpConnectionConfigurationBase http)
            return GenericResult<HttpResolvedPaging>.Failure(HttpPagingLog.PagingConnectionNotHttp(logger, connection.Name, containerName));

        return HttpPagingSettings.For(
            http.Name, http.PagingStyle, new Dictionary<string, string?>(http.AdditionalProperties, StringComparer.OrdinalIgnoreCase), containerName, style, settings, logger);
    }
}
