using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Loads;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.UnitOfWork;
using Fdw.Services.Etl.Abstractions.OptionTypes;
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
        _resolver = resolver;
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
        => RowsOf(await RunReporting(pipelineName, units, rows, parameters, now, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Runs the load units and answers what they wrote (FDW-4988): the rows loaded and, for updates and deletes that report them, the keys that matched no row.
    /// A unit whose rule is Fail has already rolled its batch back and failed with the count and a sample.
    /// </summary>
    /// <param name="pipelineName">The pipeline, for messages.</param>
    /// <param name="units">The units in the order they run.</param>
    /// <param name="rows">The pipeline's transformed output rows.</param>
    /// <param name="parameters">The run's typed parameter values, for expressions that reference them.</param>
    /// <param name="now">The moment the load began.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>What the units wrote, or the first failure.</returns>
    public async Task<IGenericResult<LoadOutcome>> RunReporting(
        string pipelineName,
        IReadOnlyList<LoadUnit> units,
        IReadOnlyList<IDictionary<string, object?>> rows,
        IReadOnlyDictionary<string, object?> parameters,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (units.Count == 0)
            return Outcome(0);
        if (rows.Count == 0 && units.Any(unit => unit.Kind.UsesRows))
        {
            PipelineRunLog.LoadStepsSkippedNoRows(_logger, pipelineName);
            return Outcome(0);
        }

        var split = await _httpLoads.Split(pipelineName, units, cancellationToken).ConfigureAwait(false);
        if (split.IsFailure || split.Value is not { } loads)
            return split.ToNewResult<LoadOutcome>();

        var planned = new List<PlannedStep>();
        foreach (var unit in loads.Database)
        {
            var steps = await Plan(unit, rows, parameters, now, cancellationToken).ConfigureAwait(false);
            if (steps.IsFailure)
                return steps.ToNewResult<LoadOutcome>();
            planned.AddRange(steps.Value!);
        }

        var interleaved = planned.Count > 0 ? FirstInterleaved(planned) : null;
        if (interleaved is not null)
            return GenericResult<LoadOutcome>.Failure(PipelineRunLog.StepsInterleaved(_logger, interleaved.Description));

        var committed = planned.Count > 0
            ? await RunGroups(pipelineName, planned, cancellationToken).ConfigureAwait(false)
            : Outcome(0);
        if (committed.IsFailure || loads.Http.Count == 0)
            return committed;

        // Why the endpoints after the database: the rows reach the endpoint only once the database loads of the run have committed.
        var sent = await _httpLoads.Write(pipelineName, loads.Http, rows, cancellationToken).ConfigureAwait(false);
        return sent.IsFailure ? sent.ToNewResult<LoadOutcome>() : GenericResult<LoadOutcome>.Success(committed.Value! with { RowsLoaded = committed.Value!.RowsLoaded + sent.Value });
    }

    private static IGenericResult<LoadOutcome> Outcome(int rowsLoaded) => GenericResult<LoadOutcome>.Success(new LoadOutcome(rowsLoaded, 0, []));

    private static IGenericResult<int> RowsOf(IGenericResult<LoadOutcome> outcome) =>
        outcome.IsFailure ? outcome.ToNewResult<int>() : GenericResult<int>.Success(outcome.Value!.RowsLoaded);

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

        var check = UnmatchedCheck(unit);
        if (check.IsFailure)
            return check.ToNewResult<List<PlannedStep>>();

        var commands = builder.Build(new LoadCommandInput(unit.Owner, unit.Row, rows, source.Value, now, _logger)
        {
            KeysStaged = unit.StageKey is not null,
            UnmatchedKeySample = unit.UnmatchedKeySample,
        });
        if (commands.IsFailure)
            return commands.ToNewResult<List<PlannedStep>>();

        var planned = commands.Value!
            .Select(command => new PlannedStep(
                command, target.Container, target.ConnectionId, target.Description, unit.Owner,
                builder.RequiredCapabilities(unit.Row), Counted: true) { CheckAnswer = command is IReportsUnmatchedKeys { UnmatchedKeySample: not null } ? check.Value : null })
            .ToList();
        if (unit.StageKey is null)
            return GenericResult<List<PlannedStep>>.Success(planned);

        var staged = await StageStep(unit, target, rows, cancellationToken).ConfigureAwait(false);
        if (staged.IsFailure)
            return staged.ToNewResult<List<PlannedStep>>();

        planned.Add(staged.Value!);
        return GenericResult<List<PlannedStep>>.Success(planned);
    }

    // The check a reporting step's answer meets before its unit commits (FDW-4988): the Fail rule refuses a batch with any key that matched no row, so the unit
    // rolls back; the Warn rule checks nothing and the destination counts what the answer says. A load that does not report has no check.
    private IGenericResult<Func<BulkWriteAnswer, IGenericMessage?>?> UnmatchedCheck(LoadUnit unit)
    {
        if (unit.UnmatchedKeySample is null)
            return GenericResult<Func<BulkWriteAnswer, IGenericMessage?>?>.Success(null);

        var rule = UnmatchedKeysRules.ByName(unit.Row.UnmatchedKeysRule ?? string.Empty);
        if (ReferenceEquals(rule, UnmatchedKeysRules.NotFound))
            return GenericResult<Func<BulkWriteAnswer, IGenericMessage?>?>.Failure(PipelineLoadLog.UnmatchedKeysRuleUnknown(_logger, unit.Owner, unit.Row.UnmatchedKeysRule ?? string.Empty));

        Func<BulkWriteAnswer, IGenericMessage?>? check = rule.FailsLoad
            ? answer => answer.UnmatchedKeys > 0 ? PipelineRunLog.UnmatchedKeysFailed(_logger, unit.Owner, answer.UnmatchedKeys, UnmatchedKeyText.Of(answer.UnmatchedSample)) : null
            : null;
        return GenericResult<Func<BulkWriteAnswer, IGenericMessage?>?>.Success(check);
    }

    // A load that stages its keys (FDW-4956) appends the batch's keys, each with the load's stage key, to its stage container in the same unit of work
    // as the batch: the stage sits on the target's connection, so the keys and the rows commit together or not at all.
    private async Task<IGenericResult<PlannedStep>> StageStep(
        LoadUnit unit, LoadTarget target, IReadOnlyList<IDictionary<string, object?>> rows, CancellationToken cancellationToken)
    {
        var stage = await _resolver.ForStage(unit, target, cancellationToken).ConfigureAwait(false);
        if (stage.IsFailure)
            return stage.ToNewResult<PlannedStep>();

        var keys = LoadColumns.Keys(unit.Row);
        var keyRows = LoadRows.Shape(unit.Owner, rows, keys, [], _logger);
        if (keyRows.IsFailure)
            return keyRows.ToNewResult<PlannedStep>();

        var present = LoadKeyFilters.KeysPresent(unit.Owner, keys, keyRows.Value!, _logger);
        if (present.IsFailure)
            return present.ToNewResult<PlannedStep>();

        foreach (var row in keyRows.Value!)
            row[unit.Row.StageKeyColumn!] = unit.StageKey;
        return GenericResult<PlannedStep>.Success(new PlannedStep(
            new InsertCommand<IDictionary<string, object?>>(keyRows.Value!), stage.Value!.Container, stage.Value.ConnectionId, stage.Value.Description, unit.Owner,
            [CommandCapabilityTypes.KeyStage.Name], Counted: false));
    }

    /// <summary>
    /// Runs the sweep of a load that staged its keys (FDW-4956): one soft delete that flags the target rows whose keys have no row in the stage under the
    /// load's stage key, refused in the same batch when the stage does not hold <paramref name="stagedRows"/> rows.
    /// </summary>
    /// <param name="pipelineName">The pipeline, for messages.</param>
    /// <param name="unit">The load; it stages its keys.</param>
    /// <param name="stagedRows">The stage rows the load committed: one for each row of each batch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The target rows flagged, or the first failure.</returns>
    public async Task<IGenericResult<int>> Sweep(string pipelineName, LoadUnit unit, long stagedRows, CancellationToken cancellationToken = default)
    {
        if (unit.StageKey is null)
            return GenericResult<int>.Failure(PipelineRunLog.StageNotNamed(_logger, unit.Owner));

        var target = await _resolver.ForUnit(unit, cancellationToken).ConfigureAwait(false);
        if (target.IsFailure)
            return target.ToNewResult<int>();
        var stage = await _resolver.ForStage(unit, target.Value!, cancellationToken).ConfigureAwait(false);
        if (stage.IsFailure)
            return stage.ToNewResult<int>();

        // The validator has refused an absent-key soft delete that does not state its flag column and value (FDW-3823).
        var row = unit.Row;
        var options = new SoftDeleteOptions(row.DeletedFlagColumn!, SoftDeleteModes.AbsentFromStage)
        {
            FlagValue = row.DeletedFlagValue!.Value,
            DeletedAtColumn = row.DeletedAtColumn,
            AbsentFromStage = new StagedKeySet(stage.Value!.Container, row.StageKeyColumn!, unit.StageKey, LoadColumns.Keys(row), stagedRows),
        };
        var problem = options.FirstProblem();
        if (problem is not null)
            return GenericResult<int>.Failure(PipelineRunLog.LoadOptionsRefused(_logger, unit.Owner, unit.Kind.Name, problem.Message));

        return RowsOf(await RunGroups(
            pipelineName,
            [new PlannedStep(new DeleteCommand { SoftDelete = options }, target.Value!.Container, target.Value.ConnectionId, target.Value.Description, unit.Owner,
                [CommandCapabilityTypes.SoftDelete.Name, CommandCapabilityTypes.KeyStage.Name], Counted: true)],
            cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Removes the stage rows of a load that stages its keys (FDW-4956): before its first batch, so a loop's later round never sees an earlier round's keys,
    /// and when it ends, however it ends.
    /// </summary>
    /// <param name="pipelineName">The pipeline, for messages.</param>
    /// <param name="unit">The load; it stages its keys.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stage rows removed, or the first failure.</returns>
    public async Task<IGenericResult<int>> ClearStage(string pipelineName, LoadUnit unit, CancellationToken cancellationToken = default)
    {
        if (unit.StageKey is null)
            return GenericResult<int>.Failure(PipelineRunLog.StageNotNamed(_logger, unit.Owner));

        var target = await _resolver.ForUnit(unit, cancellationToken).ConfigureAwait(false);
        if (target.IsFailure)
            return target.ToNewResult<int>();
        var stage = await _resolver.ForStage(unit, target.Value!, cancellationToken).ConfigureAwait(false);
        if (stage.IsFailure)
            return stage.ToNewResult<int>();

        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = unit.Row.StageKeyColumn!, Operator = FilterOperators.ByName("Equal"), Value = unit.StageKey },
        };
        return RowsOf(await RunGroups(
            pipelineName,
            [new PlannedStep(new DeleteCommand { Filter = filter }, stage.Value!.Container, stage.Value.ConnectionId, stage.Value.Description, unit.Owner,
                [CommandCapabilityTypes.KeyStage.Name], Counted: true)],
            cancellationToken).ConfigureAwait(false));
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

    private async Task<IGenericResult<LoadOutcome>> RunGroups(string pipelineName, List<PlannedStep> planned, CancellationToken cancellationToken)
    {
        var groups = planned.GroupBy(step => step.ConnectionId).ToList();
        var affected = 0;
        var unmatched = 0L;
        var sample = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var group in groups)
        {
            var ran = await RunGroup(group.First().Description, group.Key, group.ToList(), cancellationToken).ConfigureAwait(false);
            if (ran.IsFailure)
                return ran;
            affected += ran.Value!.RowsLoaded;
            unmatched += ran.Value.UnmatchedKeys;
            sample.AddRange(ran.Value.UnmatchedSample);
        }

        PipelineRunLog.LoadStepsCommitted(_logger, pipelineName, groups.Count, affected);
        return GenericResult<LoadOutcome>.Success(new LoadOutcome(affected, unmatched, sample));
    }

    private async Task<IGenericResult<LoadOutcome>> RunGroup(string description, Guid connectionId, List<PlannedStep> steps, CancellationToken cancellationToken)
    {
        if (_connections is null)
            return GenericResult<LoadOutcome>.Failure(PipelineRunLog.LoadServiceMissing(_logger, description, "connection provider"));

        var allowed = await CheckConnectionType(connectionId, steps, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
            return allowed.ToNewResult<LoadOutcome>();

        var connection = await _connections.Get(connectionId, cancellationToken).ConfigureAwait(false);
        if (connection.IsFailure)
            return connection.ToNewResult<LoadOutcome>();
        if (connection.Value is not ITransactionalDataConnection transactional)
            return GenericResult<LoadOutcome>.Failure(PipelineRunLog.ConnectionNotTransactional(_logger, description, description));

        // Why each step's figure: a load that stages its keys (FDW-4956) writes them beside its rows in the same unit, and the rows it reports are the rows
        // loaded, not the keys staged. A step that reports its unmatched keys (FDW-4988) answers them, checked before the unit commits.
        var ran = await new DataUnitOfWork()
            .RunAnswering(transactional, steps.Select(step => new UnitOfWorkStep(step.Command, step.Container) { CheckAnswer = step.CheckAnswer }).ToList(), cancellationToken)
            .ConfigureAwait(false);
        if (ran.IsFailure || ran.Value is not { } answered)
            return ran.ToNewResult<LoadOutcome>();

        var answers = answered.Answers.OfType<BulkWriteAnswer>().ToList();
        return GenericResult<LoadOutcome>.Success(new LoadOutcome(
            answered.Affected.Where((_, index) => steps[index].Counted).Sum(),
            answers.Sum(answer => answer.UnmatchedKeys),
            [.. answers.SelectMany(answer => answer.UnmatchedSample)]));
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
        IReadOnlyList<string> Capabilities,
        bool Counted)
    {
        /// <summary>Gets the check a reporting step's answer meets before its unit commits (FDW-4988); <see langword="null"/> when none.</summary>
        public Func<BulkWriteAnswer, IGenericMessage?>? CheckAnswer { get; init; }
    }
}
