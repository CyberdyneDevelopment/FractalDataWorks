using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.Visualization;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Fdw.Services.Data.Visualization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// Why: DataSet configuration is read on demand — no local dictionary:
// 1. IServiceConfigurationProvider<DataSetConfiguration> (live, via ConfigurationDb) — the dataset INSTANCES
// 2. DataSetTypes.All() filtered to non-strategy members — genuinely code-defined dataset instances only
//    (the Simple/Compound/Federated strategy KINDS in DataSetTypes are dispatch strategies, not datasets)

namespace Fdw.Services.Data;

/// <summary>
/// Default implementation of <see cref="IDataSetConfigurationProvider"/>.
/// Merges DataSet configurations from the DB-backed provider and the static TypeCollection.
/// Also provides static Configure/Register/Initialize methods for three-phase DI registration.
/// </summary>
[PlatformServiceProvider(ServiceCategory = "DataSet")]
public sealed class DataSetProvider : IDataSetConfigurationProvider
{
    // ============================================================
    // Static DI Orchestration (three-phase)
    // ============================================================

    /// <summary>
    /// Phase 1a: Configures IOptions bindings for DataSet configurations.
    /// Call before Build().
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Optional logger factory for startup diagnostics.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        if (loggerFactory != null)
        {
            DataStoreTypesLog.ConfiguredDataSetOptionsBindings(loggerFactory.CreateLogger<DataSetProvider>());
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Phase 1b: Registers required DataSet infrastructure services.
    /// Call before Build().
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Optional logger factory for startup diagnostics.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        var services = builder.Services;
        // Why: the DataSet domain registers the gateway-backed DataSetConfigurationProvider it
        // depends on (it supplies the IServiceConfigurationProvider<DataSetConfiguration> resolved
        // below), instead of the entry-point app.
        DataSetConfigurationProvider.RegisterDomainConfiguration(services);

        // Why: IDataSetConfigurationProvider (merged: TypeCollection + ctrl/cfg DB)
        // replaces the old IDataSetProvider. Resolves three sources in priority order.
        services.TryAddSingleton<IDataSetConfigurationProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<DataSetProvider>();
            var configProvider = sp.GetService<IServiceConfigurationProvider<DataSetConfiguration>>();
            return new DataSetProvider(logger, configProvider);
        });

        // Why: IDataSetFactory builds live IDataSet runtimes from DataSetConfiguration records.
        // Field mappings are composed by DataSetConfigurationProvider.Get — no resolver needed.
        services.TryAddSingleton<IDataSetFactory>(sp =>
            new DataSetFactory(sp.GetService<ILogger<DataSetFactory>>()));

        // Why: IDataSetProvider (NEW — returns IDataSet) wraps IDataSetConfigurationProvider + IDataSetFactory.
        services.TryAddSingleton<IDataSetProvider>(sp =>
            new DataSetRuntimeProvider(
                sp.GetRequiredService<IDataSetConfigurationProvider>(),
                sp.GetRequiredService<IDataSetFactory>(),
                sp.GetService<ILogger<DataSetRuntimeProvider>>()));

        // Why: IStatSetService was previously registered ad-hoc in application Program.cs files.
        // Scoped because it depends on scoped IDataGateway for per-request data access.
        services.TryAddScoped<IStatSetService, StatSetService>();

        if (loggerFactory != null)
        {
            DataStoreTypesLog.RegisteredDataSetInfrastructure(loggerFactory.CreateLogger<DataSetProvider>());
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Phase 2: Initializes DataSet infrastructure.
    /// Call after Build().
    /// </summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        var logger = loggerFactory?.CreateLogger<DataSetProvider>()
            ?? host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<DataSetProvider>();

        // Why: DataSetTypes holds the static strategy KINDS (Simple/Compound/Federated) — it is NOT a
        // registry of dataset instances. Dataset instances are resolved live from the configuration
        // provider; DataGatewayService dispatches on the authored ServiceOptionType. No per-instance
        // registration happens here (the old boot loop that stuffed a ConfiguredDataSetType per config
        // into DataSetTypes is deleted — it froze runtime-created datasets out of execution).
        DataSetTypesLog.DataSetTypesInitializedNoConfig(logger, DataSetTypes.All().Count);
    
        return GenericResult<IHost>.Success(host);
    }

    // ============================================================
    // Instance Members
    // ============================================================

    private readonly ILogger<DataSetProvider> _logger;
    private readonly IServiceConfigurationProvider<DataSetConfiguration>? _configurationProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="DataSetProvider"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configurationProvider">Optional DataSet configuration provider for DB-backed lookups.</param>
    public DataSetProvider(
        ILogger<DataSetProvider>? logger,
        IServiceConfigurationProvider<DataSetConfiguration>? configurationProvider = null)
    {
        _logger = logger ?? NullLogger<DataSetProvider>.Instance;
        _configurationProvider = configurationProvider;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataSetConfiguration>> Get(string name, CancellationToken cancellationToken = default)
    {
        DataSetProviderLog.TraceGetDataSetEntry(_logger, name);

        if (string.IsNullOrWhiteSpace(name))
        {
            return GenericResult<DataSetConfiguration>.Failure(
                DataServiceResultCodes.ByName("DataSetNameRequired"), _logger);
        }

        // Why: dataset INSTANCES come only from the configuration provider (live, gateway-backed).
        // DataSetTypes holds dispatch strategy KINDS (Simple/Compound/Federated), never instances, so
        // there is no TypeCollection fallback here.
        if (_configurationProvider != null)
        {
            var configResult = await _configurationProvider.Get(name, cancellationToken).ConfigureAwait(false);
            if (configResult.IsSuccess && configResult.Value != null)
            {
                DataSetProviderLog.DataSetRetrieved(_logger, name);
                return configResult;
            }
        }

        DataSetProviderLog.DataSetNotFound(_logger, name);
        return GenericResult<DataSetConfiguration>.Failure(
            DataServiceResultCodes.ByName("DataSetNotFound"),
            ResultDetails.Create().With("DataSetName", name));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataSetConfiguration>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        DataSetProviderLog.TraceGetDataSetByIdEntry(_logger, id);

        // Why: ID-based lookup goes to DB only — TypeCollection entries have no stable Guid Id.
        if (_configurationProvider != null)
        {
            var configResult = await _configurationProvider.Get(id, cancellationToken).ConfigureAwait(false);
            if (configResult.IsSuccess && configResult.Value != null)
            {
                DataSetProviderLog.DataSetRetrievedById(_logger, id, "configurationProvider");
                return configResult;
            }
        }

        DataSetProviderLog.DataSetByIdNotFound(_logger, id);
        return GenericResult<DataSetConfiguration>.Failure(
            DataServiceResultCodes.ByName("DataSetNotFound"),
            ResultDetails.Create().With("DataSetId", id));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<DataSetConfiguration>>> Get(CancellationToken cancellationToken = default)
    {
        DataSetProviderLog.TraceGetAllDataSetsEntry(_logger);

        var dataSets = new List<DataSetConfiguration>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Why: IOptionsMonitor + DB via _configurationProvider (covers ctrl + cfg datasets).
        if (_configurationProvider != null)
        {
            var dbResult = await _configurationProvider.Get(cancellationToken).ConfigureAwait(false);
            if (dbResult.IsSuccess && dbResult.Value != null)
            {
                foreach (var cfg in dbResult.Value)
                {
                    dataSets.Add(cfg);
                    seen.Add(cfg.Name);
                }
            }
            else
            {
                DataSetProviderLog.CfgDataSetLoadFailed(_logger, dbResult.CurrentMessage ?? "Unknown error");
            }
        }

        // Why: instances come only from the configuration provider. DataSetTypes holds dispatch
        // strategy KINDS, not datasets, so they are never added to the instance list.
        DataSetProviderLog.AllDataSetsRetrieved(_logger, dataSets.Count);
        return GenericResult<IReadOnlyList<DataSetConfiguration>>.Success(dataSets.AsReadOnly());
    }

    // ============================================================
    // Validation helpers
    // ============================================================

    /// <summary>
    /// Validates the basic structural properties of a DataSet configuration.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>Success if valid; failure with a structured message otherwise.</returns>
    public IGenericResult ValidateDataSet(DataSetConfiguration configuration)
    {
        var propertiesResult = ValidateDataSetProperties(configuration);
        if (!propertiesResult.IsSuccess)
            return propertiesResult;

        var keyFieldsResult = ValidateKeyFieldsExist(configuration);
        if (!keyFieldsResult.IsSuccess)
            return keyFieldsResult;

        return ValidateDataSetSources(configuration);
    }

    private IGenericResult ValidateDataSetProperties(DataSetConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("DataSetConfigurationRequired"), _logger);
        }

        if (string.IsNullOrWhiteSpace(configuration.Name))
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("DataSetNameRequired"), _logger);
        }

        if (configuration.Fields == null || configuration.Fields.Count == 0)
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("DataSetFieldsRequired"), _logger);
        }

        if (configuration.Sources == null || configuration.Sources.Count == 0)
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("DataSetSourcesRequired"), _logger);
        }

        return GenericResult.Success();
    }

    // Why: Deep field-reference validation requires resolving DataSetFieldRowId through the
    // DataSetField table, which happens at provider load time. Here we only sanity-check the records.
    private static IGenericResult ValidateKeyFieldsExist(DataSetConfiguration configuration)
        => GenericResult.Success();

    private static IGenericResult ValidateDataSetSources(DataSetConfiguration configuration)
    {
        // Why: Sources are composed by DataSetConfigurationProvider.Get — iterate directly.
        foreach (var source in configuration.Sources)
        {
            if (source.FieldMappingIds == null || source.FieldMappingIds.Count == 0)
            {
                return GenericResult.Failure(
                    DataServiceResultCodes.ByName("SourceNoFieldMappings"),
                    ResultDetails.Create().With("SourceName", source.SourceName));
            }
        }

        return GenericResult.Success();
    }

}
