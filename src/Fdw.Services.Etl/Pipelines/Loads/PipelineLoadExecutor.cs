using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Loads;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.UnitOfWork;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Runs a run's load units through the unit of work (FDW-1109): builds every command first, then runs the steps that share a
/// connection in one transaction, rolled back as a whole when any step fails.
/// </summary>
/// <remarks>
/// <para>
/// One transaction per connection: the steps on a connection must follow each other, because running a connection's steps
/// together would otherwise reorder them against another connection's. A plan that returns to a connection it left is
/// refused before anything runs. A later connection's failure cannot roll back an earlier one's commit; that is the
/// limit of a transaction that spans no connection, and the run fails so the watermark does not move.
/// </para>
/// <para>
/// A run that produced no rows runs nothing when any unit works on rows, so a destination is never emptied for an empty
/// source; units that take no rows (a set-based copy, a procedure, a truncate) run as stated otherwise.
/// </para>
/// </remarks>
public sealed class PipelineLoadExecutor
{
    private readonly LoadTargetResolver _resolver;
    private readonly IConnectionProvider? _connections;
    private readonly IConnectionTypeResolver? _connectionTypes;
    private readonly ILogger _logger;
    private readonly HttpRecordLoads _httpLoads;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineLoadExecutor"/> class.
    /// </summary>
    /// <param name="resolver">Resolves each load's destination and source.</param>
    /// <param name="connections">The connection provider, or <see langword="null"/> when the pipeline was built without one.</param>
    /// <param name="connectionTypes">Finds a connection's type to check the load against; <see langword="null"/> when the pipeline was built without one.</param>
    /// <param name="logger">The logger.</param>
    public PipelineLoadExecutor(
        LoadTargetResolver resolver,
        IConnectionProvider? connections,
        IConnectionTypeResolver? connectionTypes,
        ILogger? logger = null)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _connections = connections;
        _connectionTypes = connectionTypes;
        _logger = logger ?? NullLogger.Instance;
        _httpLoads = new HttpRecordLoads(_resolver, _connections, _logger);
    }

    /// <summary>Runs the load units.</summary>
    /// <param name="pipelineName">The pipeline, for messages.</param>
    /// <param name="units">The units in the order they run.</param>
    /// <param name="rows">The pipeline's transformed output rows.</param>
    /// <param name="parameters">The run's typed parameter values, for expressions that reference them.</param>
    /// <param name="now">The moment the load began.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows the commands affected, or the first failure.</returns>
    public async Task<IGenericResult<int>> Run(
        string pipelineName,
        IReadOnlyList<LoadUnit> units,
        IReadOnlyList<IDictionary<string, object?>> rows,
        IReadOnlyDictionary<string, object?> parameters,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (units.Count == 0)
            return GenericResult<int>.Success(0);
        if (rows.Count == 0 && units.Any(unit => unit.Kind.UsesRows))
        {
            PipelineRunLog.LoadStepsSkippedNoRows(_logger, pipelineName);
            return GenericResult<int>.Success(0);
        }

        var split = await _httpLoads.Split(pipelineName, units, cancellationToken).ConfigureAwait(false);
        if (split.IsFailure || split.Value is not { } loads)
            return split.ToNewResult<int>();

        var planned = new List<PlannedStep>();
        foreach (var unit in loads.Database)
        {
            var steps = await Plan(unit, rows, parameters, now, cancellationToken).ConfigureAwait(false);
            if (steps.IsFailure)
                return steps.ToNewResult<int>();
            planned.AddRange(steps.Value!);
        }

        var interleaved = planned.Count > 0 ? FirstInterleaved(planned) : null;
        if (interleaved is not null)
            return GenericResult<int>.Failure(PipelineRunLog.StepsInterleaved(_logger, interleaved.Description));

        var committed = planned.Count > 0
            ? await RunGroups(pipelineName, planned, cancellationToken).ConfigureAwait(false)
            : GenericResult<int>.Success(0);
        if (committed.IsFailure || loads.Http.Count == 0)
            return committed;

        // Why the endpoints after the database: the rows reach the endpoint only once the database loads of the run have committed.
        var sent = await _httpLoads.Write(pipelineName, loads.Http, rows, cancellationToken).ConfigureAwait(false);
        return sent.IsFailure ? sent : GenericResult<int>.Success(committed.Value + sent.Value);
    }

    private async Task<IGenericResult<List<PlannedStep>>> Plan(
        LoadUnit unit,
        IReadOnlyList<IDictionary<string, object?>> rows,
        IReadOnlyDictionary<string, object?> parameters,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var target = await _resolver.ForUnit(unit, cancellationToken).ConfigureAwait(false);
        if (target.IsFailure)
            return target.ToNewResult<List<PlannedStep>>();

        return unit.Addressing is { Splits: true } addressing
            ? await PlanFiles(unit, addressing, target.Value!, rows, parameters, now, cancellationToken).ConfigureAwait(false)
            : await PlanTarget(unit, target.Value!, rows, parameters, now, cancellationToken).ConfigureAwait(false);
    }

    // A Files destination (FDW-3193): the rows are split by the value of one field, and the unit is planned once for each file at its own address.
    // Every value is judged before any file is planned, and every file is planned before any is written, so nothing is written for a run that has a bad value.
    private async Task<IGenericResult<List<PlannedStep>>> PlanFiles(
        LoadUnit unit,
        LoadAddressing addressing,
        LoadTarget declared,
        IReadOnlyList<IDictionary<string, object?>> rows,
        IReadOnlyDictionary<string, object?> parameters,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var files = LoadFileSplit.Group(unit.Owner, ((IFilePath)declared.Container.Path).Extension, addressing.SplitField!, rows, _logger);
        if (files.IsFailure)
            return files.ToNewResult<List<PlannedStep>>();

        var planned = new List<PlannedStep>();
        foreach (var file in files.Value!)
        {
            var moved = declared.Readdressed(unit.Owner, _logger, addressing.PhysicalPath, file.FileName);
            if (moved.IsFailure)
                return moved.ToNewResult<List<PlannedStep>>();

            var steps = await PlanTarget(unit, moved.Value!, file.Rows, parameters, now, cancellationToken).ConfigureAwait(false);
            if (steps.IsFailure)
                return steps;

            planned.AddRange(steps.Value!);
        }

        return GenericResult<List<PlannedStep>>.Success(planned);
    }

    private async Task<IGenericResult<List<PlannedStep>>> PlanTarget(
        LoadUnit unit,
        LoadTarget target,
        IReadOnlyList<IDictionary<string, object?>> rows,
        IReadOnlyDictionary<string, object?> parameters,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var source = await Source(unit, target, parameters, cancellationToken).ConfigureAwait(false);
        if (source.IsFailure)
            return source.ToNewResult<List<PlannedStep>>();

        var builder = LoadStepCommandBuilders.ByName(unit.Kind.Name);
        if (ReferenceEquals(builder, LoadStepCommandBuilders.NotFound))
            return GenericResult<List<PlannedStep>>.Failure(PipelineRunLog.LoadKindUnbuildable(_logger, unit.Owner, unit.Kind.Name));

        var commands = builder.Build(new LoadCommandInput(unit.Owner, unit.Row, rows, source.Value, now, _logger));
        return commands.IsFailure
            ? commands.ToNewResult<List<PlannedStep>>()
            : GenericResult<List<PlannedStep>>.Success(commands.Value!
                .Select(command => new PlannedStep(
                    command, target.Container, target.ConnectionId, target.Description, unit.Owner,
                    builder.RequiredCapabilities(unit.Row)))
                .ToList());
    }

    // The source of a set-based copy: the source data set must sit on the destination's connection, and its filter
    // expression is read with the run's parameters filled in.
    private async Task<IGenericResult<QuerySource?>> Source(
        LoadUnit unit,
        LoadTarget destination,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        if (!unit.Kind.UsesSourceDataSet)
            return GenericResult<QuerySource?>.Success(null);

        var source = await _resolver.ForDataSet(unit.Owner, unit.Row.SourceDataSet!, cancellationToken).ConfigureAwait(false);
        if (source.IsFailure)
            return source.ToNewResult<QuerySource?>();
        if (source.Value!.ConnectionId != destination.ConnectionId)
            return GenericResult<QuerySource?>.Failure(
                PipelineRunLog.SourceNotOnSameConnection(_logger, unit.Owner, source.Value.Description, destination.Description));

        var filter = Filter(unit, parameters);
        if (filter.IsFailure)
            return filter.ToNewResult<QuerySource?>();
        var query = source.Value.AsQuerySource(unit.Owner, _logger, filter.Value);
        return query.IsFailure
            ? query.ToNewResult<QuerySource?>()
            : GenericResult<QuerySource?>.Success(query.Value);
    }

    private IGenericResult<IFilterExpression?> Filter(LoadUnit unit, IReadOnlyDictionary<string, object?> parameters)
    {
        if (string.IsNullOrWhiteSpace(unit.Row.FilterExpression))
            return GenericResult<IFilterExpression?>.Success(null);

        var text = PipelineRunParameterReferences.Resolve(unit.Owner, unit.Row.FilterExpression, parameters, _logger);
        if (text.IsFailure)
            return text.ToNewResult<IFilterExpression?>();
        var parsed = LoadFilterExpressions.Parse(unit.Owner, text.Value!, _logger);
        return parsed.IsFailure
            ? parsed.ToNewResult<IFilterExpression?>()
            : GenericResult<IFilterExpression?>.Success(parsed.Value);
    }

    // The first step that returns to a connection an earlier step left, or null when each connection's steps are together.
    private static PlannedStep? FirstInterleaved(List<PlannedStep> planned)
    {
        var left = new HashSet<Guid>();
        var current = planned[0].ConnectionId;
        foreach (var step in planned)
        {
            if (step.ConnectionId == current)
                continue;
            left.Add(current);
            current = step.ConnectionId;
            if (left.Contains(current))
                return step;
        }

        return null;
    }

    private async Task<IGenericResult<int>> RunGroups(string pipelineName, List<PlannedStep> planned, CancellationToken cancellationToken)
    {
        var groups = planned.GroupBy(step => step.ConnectionId).ToList();
        var affected = 0;
        foreach (var group in groups)
        {
            var ran = await RunGroup(group.First().Description, group.Key, group.ToList(), cancellationToken).ConfigureAwait(false);
            if (ran.IsFailure)
                return ran;
            affected += ran.Value;
        }

        PipelineRunLog.LoadStepsCommitted(_logger, pipelineName, groups.Count, affected);
        return GenericResult<int>.Success(affected);
    }

    private async Task<IGenericResult<int>> RunGroup(string description, Guid connectionId, List<PlannedStep> steps, CancellationToken cancellationToken)
    {
        if (_connections is null)
            return GenericResult<int>.Failure(PipelineRunLog.LoadServiceMissing(_logger, description, "connection provider"));

        var allowed = await CheckConnectionType(connectionId, steps, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
            return allowed.ToNewResult<int>();

        var connection = await _connections.Get(connectionId, cancellationToken).ConfigureAwait(false);
        if (connection.IsFailure)
            return connection.ToNewResult<int>();
        if (connection.Value is not ITransactionalDataConnection transactional)
            return GenericResult<int>.Failure(PipelineRunLog.ConnectionNotTransactional(_logger, description, description));

        var unitOfWork = new DataUnitOfWork();
        return await unitOfWork
            .Run(transactional, steps.Select(step => new UnitOfWorkStep(step.Command, step.Container)).ToList(), cancellationToken)
            .ConfigureAwait(false);
    }

    // Before a transaction opens: the connection's type must list every capability the group's loads need, and the unit of
    // work itself. Nothing runs against a connection that cannot.
    private async Task<IGenericResult> CheckConnectionType(Guid connectionId, List<PlannedStep> steps, CancellationToken cancellationToken)
    {
        if (_connectionTypes is null)
            return GenericResult.Failure(PipelineRunLog.LoadServiceMissing(_logger, steps[0].Description, "connection type resolver"));

        var type = await _connectionTypes.ForConnection(connectionId, cancellationToken).ConfigureAwait(false);
        if (type.IsFailure)
            return type;

        var listed = type.Value!.SupportedCommands.Select(capability => capability.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var step in steps)
        {
            var missing = step.Capabilities.Append(CommandCapabilityTypes.UnitOfWork.Name).FirstOrDefault(name => !listed.Contains(name));
            if (missing is not null)
                return GenericResult.Failure(PipelineRunLog.ConnectionLacksCapability(_logger, step.Owner, step.Description, missing));
        }

        return GenericResult.Success();
    }

    private sealed record PlannedStep(
        IDataCommand Command,
        IDataContainer Container,
        Guid ConnectionId,
        string Description,
        string Owner,
        IReadOnlyList<string> Capabilities);
}
