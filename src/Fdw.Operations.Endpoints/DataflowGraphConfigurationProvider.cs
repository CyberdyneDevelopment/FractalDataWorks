using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Owns all ConfigurationDb gateway access for the dataflow graph endpoint.
/// Reads DataSet, DataStore, DataSetSource, and Pipeline header rows for graph construction.
/// The endpoint injects this provider — never IConfigurationGateway directly.
/// </summary>
public class DataflowGraphConfigurationProvider
{
    /// <summary>
    /// Registers <see cref="DataflowGraphConfigurationProvider"/> as a singleton in the DI container.
    /// Call from Program.cs after the IConfigurationGateway is registered.
    /// </summary>
    public static IServiceCollection RegisterDomainConfiguration(IServiceCollection services)
    {
        // Why: Singleton — the provider holds no per-request state; IConfigurationGateway is scoped
        // but accessed via a factory so the singleton wrapper remains safe.
        services.TryAddSingleton<DataflowGraphConfigurationProvider>(sp =>
            new DataflowGraphConfigurationProvider(
                sp.GetRequiredService<IConfigurationGatewayProvider>()
                    .Get(EtlPipelineTypes.ConfigurationConnection).Value!,
                sp.GetService<ILogger<DataflowGraphConfigurationProvider>>()));
        return services;
    }


    private const string DataStoreName = "ConfigurationDb";
    private const string DataPath = "data";
    private const string PipePath = "pipe";

    private readonly IConfigurationGateway _gateway;
    private readonly ILogger<DataflowGraphConfigurationProvider> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DataflowGraphConfigurationProvider"/>.
    /// </summary>
    public DataflowGraphConfigurationProvider(
        IConfigurationGateway gateway,
        ILogger<DataflowGraphConfigurationProvider>? logger = null)
    {
        _gateway = gateway;
        _logger = logger ?? NullLogger<DataflowGraphConfigurationProvider>.Instance;
    }

    /// <summary>
    /// Returns all current DataSet summary rows for graph node construction.
    /// </summary>
    // Why: virtual — Moq can override in unit tests without a real IConfigurationGateway.
    public virtual async Task<IGenericResult<IReadOnlyList<DataSetRecord>>> LoadDataSets(
        CancellationToken cancellationToken = default)
    {
        // Why: Addressing moved off IDataCommand onto DataStoreTarget.
        var command = new QueryCommand<DataSetRecord>();

        var result = await _gateway.Execute<IEnumerable<DataSetRecord>>(
                command, new DataStoreTarget(DataStoreName, DataPath, "DataSet"), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return result.ToNewResult<IReadOnlyList<DataSetRecord>>();

        return GenericResult<IReadOnlyList<DataSetRecord>>.Success(result.Value?.ToList() ?? []);
    }

    /// <summary>
    /// Returns all current DataStore summary rows for graph node construction.
    /// </summary>
    // Why: virtual — Moq can override in unit tests without a real IConfigurationGateway.
    public virtual async Task<IGenericResult<IReadOnlyList<DataStoreRecord>>> LoadDataStores(
        CancellationToken cancellationToken = default)
    {
        // Why: Addressing moved off IDataCommand onto DataStoreTarget.
        var command = new QueryCommand<DataStoreRecord>();

        var result = await _gateway.Execute<IEnumerable<DataStoreRecord>>(
                command, new DataStoreTarget(DataStoreName, DataPath, "DataStoreConfiguration"), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return result.ToNewResult<IReadOnlyList<DataStoreRecord>>();

        return GenericResult<IReadOnlyList<DataStoreRecord>>.Success(result.Value?.ToList() ?? []);
    }

    /// <summary>
    /// Returns all current DataSetSource summary rows for graph edge construction.
    /// </summary>
    // Why: virtual — Moq can override in unit tests without a real IConfigurationGateway.
    public virtual async Task<IGenericResult<IReadOnlyList<DataSetSourceConfiguration>>> LoadSources(
        CancellationToken cancellationToken = default)
    {
        // Why: Addressing moved off IDataCommand onto DataStoreTarget.
        var command = new QueryCommand<DataSetSourceConfiguration>();

        var result = await _gateway.Execute<IEnumerable<DataSetSourceConfiguration>>(
                command, new DataStoreTarget(DataStoreName, DataPath, "DataSetSource"), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return result.ToNewResult<IReadOnlyList<DataSetSourceConfiguration>>();

        return GenericResult<IReadOnlyList<DataSetSourceConfiguration>>.Success(result.Value?.ToList() ?? []);
    }

    /// <summary>
    /// Returns whether a pipeline with the given name exists (case-insensitive).
    /// Used to validate the optional ?pipelineName filter before graph construction.
    /// </summary>
    // Why: virtual — Moq can override in unit tests without a real IConfigurationGateway.
    public virtual async Task<IGenericResult<bool>> PipelineExists(
        string name, CancellationToken cancellationToken = default)
    {
        try
        {
            // Why: Addressing moved off IDataCommand onto DataStoreTarget.
            var command = new QueryCommand<System.Collections.Generic.Dictionary<string, object?>>();

            var result = await _gateway
                .Execute<IEnumerable<System.Collections.Generic.Dictionary<string, object?>>>(
                    command, new DataStoreTarget(DataStoreName, PipePath, "Pipeline"), cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
                return result.ToNewResult<bool>();

            var exists = (result.Value ?? []).Any(p =>
                p.TryGetValue("Name", out var n)
                && n is string s
                && string.Equals(s, name, System.StringComparison.OrdinalIgnoreCase));

            return GenericResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                DataflowGraphConfigurationProviderLog.PipelineFilterCheckFailed(_logger, ex));
        }
    }
}
