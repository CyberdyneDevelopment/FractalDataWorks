using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Execution;

/// <summary>
/// Concrete execution context handed to a dataset strategy (<see cref="SimpleDataSetType"/>,
/// <see cref="CompoundDataSetType"/>, <see cref="FederatedDataSetType"/>) so it can pull and join the
/// dataset's sources without taking a constructor dependency.
/// </summary>
/// <remarks>
/// Why: dataset strategy type-options are module-init singletons with a parameterless constructor and
/// no DI. <see cref="DataGatewayService"/> (which DOES have the providers injected) builds this per
/// call and the strategy downcasts <see cref="IDataSetExecutionContext"/> to read these members. The
/// strategies are stateless — every dependency they need for a single execution lives here.
/// </remarks>
public sealed class DataSetExecutionContext : IDataSetExecutionContext
{
    /// <summary>Initializes a new instance of the <see cref="DataSetExecutionContext"/> class.</summary>
    /// <param name="config">The resolved (composed) dataset configuration to execute against.</param>
    /// <param name="connectionProvider">Resolves named <see cref="IDataConnection"/> instances.</param>
    /// <param name="dataStoreProvider">Resolves containers via the DataStore → Path → Container tree.</param>
    /// <param name="pushdown">Translates logical filters to physical for predicate pushdown.</param>
    /// <param name="logger">Logger for execution diagnostics.</param>
    public DataSetExecutionContext(
        DataSetConfiguration config,
        IDataConnectionProvider connectionProvider,
        IDataStoreProvider dataStoreProvider,
        PredicatePushdownAnalyzer pushdown,
        ILogger logger)
    {
        Config = config;
        ConnectionProvider = connectionProvider;
        DataStoreProvider = dataStoreProvider;
        Pushdown = pushdown;
        Logger = logger;
    }

    /// <inheritdoc />
    IGenericConfiguration IDataSetExecutionContext.Configuration => Config;

    /// <summary>Gets the resolved (composed) dataset configuration this execution runs against.</summary>
    public DataSetConfiguration Config { get; }

    /// <summary>Gets the provider that resolves named connections.</summary>
    public IDataConnectionProvider ConnectionProvider { get; }

    /// <summary>Gets the provider that resolves containers from the DataStore tree.</summary>
    public IDataStoreProvider DataStoreProvider { get; }

    /// <summary>Gets the predicate-pushdown analyzer for logical→physical filter translation.</summary>
    public PredicatePushdownAnalyzer Pushdown { get; }

    /// <summary>Gets the execution logger.</summary>
    public ILogger Logger { get; }
}
