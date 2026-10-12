using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions.CommandCapabilities;
using Fdw.Services.Data;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Checks an Execute load step against what its destination can run (FDW-1110, contracts v3 and v3.1): the data store is
/// one the destination data set sits on, its connection type declares the Execute capability, the container is one the
/// store knows and the connection type can execute, and the parameters are fields of the container. Nothing here names a
/// connection type or a container type: the connection type says what it executes.
/// </summary>
public sealed class PipelineExecuteStepCheck
{
    private readonly DataStoreConfigurationProvider _dataStores;
    private readonly IConnectionConfigurationProvider _connections;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineExecuteStepCheck"/> class.</summary>
    /// <param name="dataStores">The data store configurations the container is read from.</param>
    /// <param name="connections">The connection configurations the destination's connection type is read from.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    public PipelineExecuteStepCheck(DataStoreConfigurationProvider dataStores, IConnectionConfigurationProvider connections, ILogger logger)
    {
        _dataStores = dataStores;
        _connections = connections;
        _logger = logger;
    }

    /// <summary>Checks one Execute step.</summary>
    /// <param name="row">The step's load row.</param>
    /// <param name="destination">The step's destination data set.</param>
    /// <param name="owner">What the problems say the step is.</param>
    /// <param name="stage">The stage the problems are placed on.</param>
    /// <param name="problems">Where the problems are added.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>A task that completes when the step has been checked.</returns>
    public async Task Check(
        PipelineLoadStepConfiguration row,
        IDataSetImplementationConfiguration destination,
        string owner,
        string? stage,
        List<PipelineProblem> problems,
        CancellationToken cancellationToken = default)
    {
        var sources = destination.Sources.Where(s => Same(s.DataStoreName, row.DestinationDataStoreName)).ToList();
        if (sources.Count == 0)
        {
            problems.Add(Problem(stage, "Options.DestinationContainer.DataStoreName", PipelineEndpointLog.ExecuteStoreNotOnDataSet(
                _logger, owner, row.DestinationDataStoreName!, destination.Name, string.Join(", ", destination.Sources.Select(s => s.DataStoreName).Distinct(StringComparer.OrdinalIgnoreCase)))));
            return;
        }

        // The connection type is the one the engine resolves for a connection: the Implementation of the connection's own
        // configuration (RegisteredConnectionTypeResolver), not a name copied onto the data set's source.
        var connection = await _connections.Get(sources[0].ConnectionName, cancellationToken).ConfigureAwait(false);
        if (connection.IsFailure || connection.Value is null)
        {
            problems.Add(Problem(stage, "DestinationDataSet", PipelineEndpointLog.ExecuteConnectionUnreadable(
                _logger, owner, sources[0].ConnectionName, string.Concat(connection.CurrentMessage))));
            return;
        }

        var connectionType = ConnectionTypes.ByName(connection.Value.Implementation);
        if (ReferenceEquals(connectionType, ConnectionTypes.NotFound))
        {
            problems.Add(Problem(stage, "DestinationDataSet", PipelineEndpointLog.ExecuteConnectionTypeUnregistered(
                _logger, owner, connection.Value.Name, connection.Value.Implementation)));
            return;
        }

        if (!connectionType.SupportedCommands.Any(c => c is ExecuteCapability))
        {
            problems.Add(Problem(stage, "DestinationDataSet", PipelineEndpointLog.ExecuteNotSupported(
                _logger, owner, connectionType.Name, string.Join(", ", connectionType.SupportedCommands.Select(c => c.Name)))));
            return;
        }

        await CheckContainer(row, connectionType, owner, stage, problems, cancellationToken).ConfigureAwait(false);
    }

    private async Task CheckContainer(
        PipelineLoadStepConfiguration row,
        IConnectionType connectionType,
        string owner,
        string? stage,
        List<PipelineProblem> problems,
        CancellationToken cancellationToken)
    {
        var store = await _dataStores.Get(row.DestinationDataStoreName!, ConfigurationLevels.WholeAggregate, cancellationToken).ConfigureAwait(false);
        if (store.IsFailure || store.Value is null)
        {
            problems.Add(Problem(stage, "Options.DestinationContainer.DataStoreName", PipelineEndpointLog.ExecuteStoreUnreadable(
                _logger, owner, row.DestinationDataStoreName!, string.Concat(store.CurrentMessage))));
            return;
        }

        var container = store.Value.Paths
            .Where(p => Same(p.Name, row.DestinationPathName))
            .SelectMany(p => p.Containers)
            .FirstOrDefault(c => Same(c.Name, row.DestinationContainerName));
        if (container is null)
        {
            problems.Add(Problem(stage, "Options.DestinationContainer", PipelineEndpointLog.ExecuteContainerUnknown(
                _logger, owner, row.DestinationDataStoreName!, row.DestinationContainerName!, row.DestinationPathName!)));
            return;
        }

        var executable = connectionType is ISupportsExecutableContainerTypes executeCapable ? executeCapable.ExecutableContainerTypes : (IReadOnlyList<string>)[];
        if (container.TypeId is not { } containerType || !executable.Contains(containerType, StringComparer.OrdinalIgnoreCase))
        {
            problems.Add(Problem(stage, "Options.DestinationContainer", PipelineEndpointLog.ExecuteContainerNotExecutable(
                _logger, owner, container.Name, string.Concat(container.TypeId), connectionType.Name, string.Join(", ", executable))));
            return;
        }

        CheckParameters(row, container, owner, stage, problems);
    }

    private void CheckParameters(
        PipelineLoadStepConfiguration row,
        DataContainerConfiguration container,
        string owner,
        string? stage,
        List<PipelineProblem> problems)
    {
        var supplied = row.ValueSources.Select(s => Bare(s.ColumnName)).ToList();
        foreach (var name in supplied.Where(n => !container.Fields.Any(f => Same(Bare(f.Name), n))))
        {
            problems.Add(Problem(stage, "Options.Parameters", PipelineEndpointLog.ExecuteParameterNotField(_logger, owner, name, container.Name)));
        }

        foreach (var field in container.Fields.Where(f => !f.IsNullable && !f.HasDefault && !supplied.Contains(Bare(f.Name), StringComparer.OrdinalIgnoreCase)))
        {
            problems.Add(Problem(stage, "Options.Parameters", PipelineEndpointLog.ExecuteParameterMissing(_logger, owner, container.Name, Bare(field.Name))));
        }
    }

    // A parameter is named with or without a leading @ (contract v4 item 6).
    private static string Bare(string name) => name.TrimStart('@');

    private static bool Same(string? left, string? right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static PipelineProblem Problem(string? stage, string field, IGenericMessage message) =>
        new() { Stage = stage, Field = field, Reason = message.Message };
}
