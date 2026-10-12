using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Checks a pipeline's load definition against the schemas it touches (FDW-1110, contract item 6): every column the
/// definition names exists on the data set it loads, a set-based copy reads a data set on the destination's connection, an
/// Execute step names a container its destination can run, and every <c>{{name}}</c> a filter references is a declared run
/// parameter. The structure of the definition is <see cref="PipelineLoadValidator"/>'s; this is what needs the data sets.
/// </summary>
public sealed class PipelineLoadSchemaCheck
{

    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly PipelineExecuteStepCheck _execute;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineLoadSchemaCheck"/> class.</summary>
    /// <param name="dataSets">The data set configurations the columns are read from.</param>
    /// <param name="dataStores">The data store configurations an Execute step's container is read from.</param>
    /// <param name="connections">The connection configurations an Execute step's connection type is read from.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    public PipelineLoadSchemaCheck(
        IDataSetConfigurationProvider dataSets, DataStoreConfigurationProvider dataStores, IConnectionConfigurationProvider connections, ILogger logger)
    {
        _dataSets = dataSets;
        _execute = new PipelineExecuteStepCheck(dataStores, connections, logger);
        _logger = logger;
    }

    /// <summary>Checks a mapped load definition.</summary>
    /// <param name="load">The definition as configuration rows.</param>
    /// <param name="pipelineDestinationDataSet">The pipeline's destination data set, which its own load writes to.</param>
    /// <param name="expressions">The filter expressions of the pipeline's transforms, each with the stage it sits on.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>Every problem found; empty when the definition fits the schemas.</returns>
    public async Task<IReadOnlyList<PipelineProblem>> Check(
        MappedPipelineLoad load,
        string? pipelineDestinationDataSet,
        IReadOnlyList<(string? Stage, string? Expression)> expressions,
        CancellationToken cancellationToken = default)
    {
        var problems = new List<PipelineProblem>();
        if (load.LoadSteps is { } rows)
        {
            foreach (var row in rows)
            {
                await CheckRow(row, pipelineDestinationDataSet, problems, cancellationToken).ConfigureAwait(false);
            }
        }

        CheckReferences(load, expressions, problems);
        return problems;
    }

    private async Task CheckRow(
        PipelineLoadStepConfiguration row,
        string? pipelineDestinationDataSet,
        List<PipelineProblem> problems,
        CancellationToken cancellationToken)
    {
        var context = LoadRowContext.Of(row, pipelineDestinationDataSet);
        var columns = LoadRowColumns.OfDestination(row, context.Prefix, context.Executes).ToList();
        if (columns.Count == 0 && !context.Executes)
            return;
        if (string.IsNullOrWhiteSpace(context.DestinationName))
        {
            problems.Add(Problem(context.Stage, context.Prefix, PipelineEndpointLog.LoadNeedsDestinationDataSet(_logger, context.Owner)));
            return;
        }

        var destination = await Resolve(context.DestinationName, context, problems, cancellationToken).ConfigureAwait(false);
        if (destination is not null)
            await CheckDestination(context, destination, columns, problems, cancellationToken).ConfigureAwait(false);
    }

    private async Task CheckDestination(
        LoadRowContext context,
        IDataSetImplementationConfiguration destination,
        List<NamedColumn> columns,
        List<PipelineProblem> problems,
        CancellationToken cancellationToken)
    {
        var row = context.Row;
        if (!context.Executes && SingleSourceProblem(destination) is { } unusable)
            problems.Add(Problem(context.Stage, "DestinationDataSet", PipelineEndpointLog.LoadDataSetUnusable(_logger, context.Owner, destination.Name, unusable)));
        var copies = !string.IsNullOrWhiteSpace(row.SourceDataSet);
        // A set-based copy orders its sequence by columns of its source; every other step orders by the rows' own columns.
        if (!copies)
            columns.AddRange(LoadRowColumns.SequenceOrderOfDestination(row));
        CheckFields(destination, columns, context, problems);
        if (copies)
            await CheckSource(context, destination, problems, cancellationToken).ConfigureAwait(false);
        if (context.Executes)
            await _execute.Check(row, destination, context.Owner, context.Stage, problems, cancellationToken).ConfigureAwait(false);
    }

    private async Task CheckSource(
        LoadRowContext context,
        IDataSetImplementationConfiguration destination,
        List<PipelineProblem> problems,
        CancellationToken cancellationToken)
    {
        var source = await Resolve(context.Row.SourceDataSet!, context, problems, cancellationToken).ConfigureAwait(false);
        if (source is null)
            return;

        if (SingleSourceProblem(source) is { } unusable)
            problems.Add(Problem(context.Stage, $"{context.Prefix}.SourceDataSet", PipelineEndpointLog.LoadDataSetUnusable(_logger, context.Owner, source.Name, unusable)));

        var connections = destination.Sources.Select(s => s.ConnectionName).ToList();
        if (!source.Sources.Any(s => connections.Contains(s.ConnectionName, StringComparer.OrdinalIgnoreCase)))
        {
            problems.Add(Problem(context.Stage, $"{context.Prefix}.SourceDataSet", PipelineEndpointLog.LoadSourceNotOnDestinationConnection(
                _logger, context.Owner, source.Name, destination.Name)));
        }

        CheckFields(source, LoadRowColumns.OfSource(context.Row, context.Prefix), context, problems);
    }

    // What the engine's load target resolver requires of a data set a load reads or writes (LoadTargetResolver.ForDataSet):
    // exactly one source, and that source names a container. Said here too so a draft that would fail at run is refused now.
    private static string? SingleSourceProblem(IDataSetImplementationConfiguration dataSet)
    {
        if (dataSet.Sources.Count != 1)
            return $"a data set a load reads or writes has exactly one source, and this one has {dataSet.Sources.Count.ToString(CultureInfo.InvariantCulture)}";
        return string.IsNullOrWhiteSpace(dataSet.Sources[0].ContainerName) ? "its source names no container" : null;
    }

    private async Task<IDataSetImplementationConfiguration?> Resolve(
        string name, LoadRowContext context, List<PipelineProblem> problems, CancellationToken cancellationToken)
    {
        var dataSet = await _dataSets.Get(name, cancellationToken).ConfigureAwait(false);
        if (dataSet.IsSuccess && dataSet.Value is not null)
            return dataSet.Value;

        problems.Add(Problem(context.Stage, "DestinationDataSet", PipelineEndpointLog.LoadDataSetNotVisible(_logger, context.Owner, name)));
        return null;
    }

    private void CheckFields(
        IDataSetImplementationConfiguration dataSet,
        IEnumerable<NamedColumn> columns,
        LoadRowContext context,
        List<PipelineProblem> problems)
    {
        foreach (var column in columns.Where(c => !dataSet.Fields.Any(f => string.Equals(f.Name, c.Column, StringComparison.OrdinalIgnoreCase))))
        {
            problems.Add(Problem(context.Stage, column.Field, PipelineEndpointLog.LoadColumnNotOnDataSet(_logger, context.Owner, column.Column, dataSet.Name)));
        }
    }

    private void CheckReferences(
        MappedPipelineLoad load,
        IReadOnlyList<(string? Stage, string? Expression)> expressions,
        List<PipelineProblem> problems)
    {
        var declared = new List<string> { PipelineRunParameterValues.LastSuccessfulRun };
        if (load.RunParameters is { } parameters)
            declared.AddRange(parameters.Select(p => p.Name));

        var all = new List<(string? Stage, string? Expression)>(expressions);
        if (load.LoadSteps is { } rows)
            all.AddRange(rows.Where(r => r.StepOrder is not null).Select(r => ((string?)r.Name, r.FilterExpression)));

        foreach (var (stage, expression) in all.Where(e => !string.IsNullOrWhiteSpace(e.Expression)))
        {
            CheckExpression(stage, expression!, declared, problems);
        }
    }

    // The names come from the engine's own reference reader, so a reference is found here exactly as a run finds it; the
    // engine matches names ignoring case.
    private void CheckExpression(string? stage, string expression, List<string> declared, List<PipelineProblem> problems)
    {
        foreach (var name in PipelineRunParameterReferences.Names(expression).Where(n => !declared.Contains(n, StringComparer.OrdinalIgnoreCase)))
        {
            problems.Add(Problem(stage, "FilterExpression", PipelineEndpointLog.ParameterReferenceUndeclared(
                _logger, stage is null ? "the pipeline" : $"stage '{stage}'", name)));
        }
    }

    // Why the path is set here as well: the structure checks place a step's problem on [step name], so a problem the schema check
    // finds on the same step must carry the same path and not none (FDW-1150).
    private static PipelineProblem Problem(string? stage, string field, IGenericMessage message) =>
        new() { Stage = stage, StagePath = stage is null ? null : new[] { stage }, Field = field, Reason = message.Message };
}
