using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Walks the ctrl-tier <see cref="IDataStore"/> tree to resolve configuration containers
/// by type name or by section path (category).
/// </summary>
/// <remarks>
/// Replaces the deleted <c>IConfigurationType</c> path as the authoritative source of
/// schema and table metadata for configuration endpoints.
/// </remarks>
public class ConfigurationContainerLookup : IConfigurationContainerLookup
{
    private readonly Lazy<IReadOnlyList<IDataStore>> _dataStores;
    private readonly ILogger<ConfigurationContainerLookup> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationContainerLookup"/> class.
    /// </summary>
    /// <param name="dataStores">The lazy ctrl-tier data store tree built at startup.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ConfigurationContainerLookup(
        Lazy<IReadOnlyList<IDataStore>> dataStores,
        ILogger<ConfigurationContainerLookup>? logger = null)
    {
        _dataStores = dataStores;
        _logger = logger ?? NullLogger<ConfigurationContainerLookup>.Instance;
    }

    /// <inheritdoc />
    public IGenericResult<IDataContainer> Get(string configTypeName)
    {
        ConfigurationContainerLookupLog.LookupStarted(_logger, configTypeName);

        foreach (var store in _dataStores.Value)
        {
            foreach (var path in store.Paths)
            {
                foreach (var container in path.Containers)
                {
                    if (string.Equals(container.Name, configTypeName, StringComparison.OrdinalIgnoreCase))
                    {
                        ConfigurationContainerLookupLog.ContainerResolved(
                            _logger, configTypeName, store.Name, path.Name);
                        return GenericResult<IDataContainer>.Success(container);
                    }
                }
            }
        }

        return GenericResult<IDataContainer>.Failure(
            ConfigurationContainerLookupLog.ContainerNotFound(_logger, configTypeName));
    }

    /// <inheritdoc />
    public IReadOnlyList<IDataContainer> All()
    {
        var result = new List<IDataContainer>();
        foreach (var store in _dataStores.Value)
        {
            foreach (var path in store.Paths)
            {
                result.AddRange(path.Containers);
            }
        }
        ConfigurationContainerLookupLog.AllResult(_logger, result.Count);
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<IDataContainer> ByCategory(string sectionPath)
    {
        // Why: configuration categories are derived from the container's schema (Path.Name) —
        // the same proxy used by GetConfigurationCategoriesEndpointBase.MapCategories and
        // ListConfigurationInstancesEndpointBase.MapToSummary. Filtering containers by Path.Name
        // is therefore the consistent, fully-general implementation across all schemas
        // (conn, data, sched, theme, settings, …) — no per-category special-casing.
        var result = new List<IDataContainer>();
        foreach (var store in _dataStores.Value)
        {
            foreach (var path in store.Paths)
            {
                if (!string.Equals(path.Name, sectionPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.AddRange(path.Containers);
            }
        }

        ConfigurationContainerLookupLog.ByCategoryResult(_logger, sectionPath, result.Count);
        return result;
    }
}
