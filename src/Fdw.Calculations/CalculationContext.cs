using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Results;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Calculations;

/// <summary>
/// Default implementation of <see cref="ICalculationContext"/>.
/// Provides access to data sources and manages execution state.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationContext : ICalculationContext
{
    private readonly Dictionary<string, object?> _sharedState;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationContext"/> class.
    /// </summary>
    /// <param name="dataGateways">The data gateway for accessing data sources.</param>
    /// <param name="parameters">Optional parameters for the calculation.</param>
    /// <param name="services">Optional service provider for dependency resolution.</param>
    /// <param name="logger">Optional logger for this execution.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public CalculationContext(
        IDataGatewayProvider dataGateways,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IServiceProvider? services = null,
        ILogger<CalculationContext>? logger = null,
        CancellationToken cancellationToken = default)
    {
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        ExecutionId = Guid.NewGuid();
        StartTime = DateTimeOffset.UtcNow;
        Parameters = parameters ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        Services = services ?? EmptyServiceProvider.Instance;
        Logger = logger ?? NullLogger<CalculationContext>.Instance;
        CancellationToken = cancellationToken;
        _sharedState = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public Guid ExecutionId { get; }

    /// <inheritdoc/>
    public DateTimeOffset StartTime { get; }

    /// <inheritdoc/>
    private readonly IDataGatewayProvider _dataGateways;

    /// <summary>Gets the gateway this calculation reads and writes through.</summary>
    /// <remarks>Resolved per use: the gateway is scoped and a context is not, so holding one
    /// would be a captive dependency.</remarks>
    public IDataGateway DataGateway => _dataGateways.ByName("Main");

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <inheritdoc/>
    public IDictionary<string, object?> SharedState => _sharedState;

    /// <inheritdoc/>
    public IServiceProvider Services { get; }

    /// <inheritdoc/>
    public ILogger Logger { get; }

    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc/>
    public Task<IGenericResult<TData>> GetData<TData>(
        string connectionName,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            return Task.FromResult(GenericResult<TData>.Failure(CalculationResultCodes.ConnectionNameRequired()));

        if (string.IsNullOrWhiteSpace(containerName))
            return Task.FromResult(GenericResult<TData>.Failure(CalculationResultCodes.ContainerNameRequired()));

        return Task.FromResult(GenericResult<TData>.Failure(CalculationResultCodes.UseDataCommandOverload()));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TData>> GetData<TData>(
        IDataCommand command,
        DataStoreTarget target,
        CancellationToken cancellationToken = default)
    {
        if (command == null)
            return GenericResult<TData>.Failure(CalculationResultCodes.CommandRequired());

        return await DataGateway.Execute<TData>(command, target, cancellationToken).ConfigureAwait(false);
    }

    #region Dataset Convenience Methods

    private const string DataSetPrefix = "DataSet:";

    /// <inheritdoc/>
    public T? GetDataSet<T>(string name)
    {
        var key = DataSetPrefix + name;
        return _sharedState.TryGetValue(key, out var value) && value is T typedValue
            ? typedValue
            : default;
    }

    /// <inheritdoc/>
    public bool TryGetDataSet<T>(string name, [NotNullWhen(true)] out T? dataset)
    {
        var key = DataSetPrefix + name;
        if (_sharedState.TryGetValue(key, out var value) && value is T typedValue)
        {
            dataset = typedValue;
            return true;
        }

        dataset = default;
        return false;
    }

    /// <inheritdoc/>
    public void SetDataSet<T>(string name, T dataset)
    {
        _sharedState[DataSetPrefix + name] = dataset;
    }

    /// <inheritdoc/>
    public bool HasDataSet(string name) => _sharedState.ContainsKey(DataSetPrefix + name);

    #endregion

    #region Calculation Result Convenience Methods

    private const string CalculationPrefix = "Calculation:";

    /// <inheritdoc/>
    public T? GetCalculationResult<T>(string calculationName)
    {
        var key = CalculationPrefix + calculationName;
        return _sharedState.TryGetValue(key, out var value) && value is T typedValue
            ? typedValue
            : default;
    }

    /// <inheritdoc/>
    public bool TryGetCalculationResult<T>(string calculationName, [NotNullWhen(true)] out T? result)
    {
        var key = CalculationPrefix + calculationName;
        if (_sharedState.TryGetValue(key, out var value) && value is T typedValue)
        {
            result = typedValue;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc/>
    public void SetCalculationResult<T>(string calculationName, T result)
    {
        _sharedState[CalculationPrefix + calculationName] = result;
    }

    /// <inheritdoc/>
    public bool HasCalculationResult(string calculationName) =>
        _sharedState.ContainsKey(CalculationPrefix + calculationName);

    #endregion

    /// <summary>
    /// Minimal service provider returned when no DI container is available.
    /// </summary>
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        /// <summary>Gets the singleton instance.</summary>
        public static readonly EmptyServiceProvider Instance = new();

        /// <inheritdoc/>
        public object? GetService(Type serviceType) => null;
    }
}
