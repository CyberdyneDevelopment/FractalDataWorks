using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A destination component that loads each batch it receives through a pipeline's own load mechanism (FDW-1386): a
/// <see cref="LoadUnit"/> run by the <see cref="PipelineLoadExecutor"/>, so insert, upsert and the options of a load mean the
/// same here as they do in a pipeline. It has no output node.
/// </summary>
/// <remarks>
/// <para>
/// Each batch runs in one unit of work. A destination that replaces (load mode Replace) names a truncate unit; it runs once,
/// immediately before the first batch is written, so a source that yields nothing never empties the target.
/// </para>
/// <para>
/// A load that must see the whole output at once (a load that states a soft delete, which flags the keys absent from the rows it
/// is given) holds its batches back and loads them once, after its input ends, the way the load of a pipeline does (FDW-1647).
/// </para>
/// </remarks>
public sealed class FlowLoadDestination : IFlowComponent
{
    /// <summary>The name of the destination's one input node.</summary>
    public const string InputName = FlowDestination.InputName;

    private readonly string _name;
    private readonly PipelineLoadExecutor _executor;
    private readonly string _pipelineName;
    private readonly LoadUnit _unit;
    private readonly LoadUnit? _truncateFirst;
    private readonly IReadOnlyDictionary<string, object?> _parameters;
    private readonly DateTimeOffset _startedAt;
    private readonly ILogger _logger;
    private readonly bool _skipWrites;

    /// <summary>Initializes a new instance of the <see cref="FlowLoadDestination"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="executor">The executor that runs a load unit.</param>
    /// <param name="pipelineName">The pipeline the run belongs to, for the executor's messages.</param>
    /// <param name="unit">The load each batch goes through.</param>
    /// <param name="truncateFirst">A truncate of the same target, run once before the first batch; <see langword="null"/> when the target is not replaced.</param>
    /// <param name="parameters">The run's typed parameter values, for expressions that reference them.</param>
    /// <param name="startedAt">The moment the run began.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="skipWrites">Whether this is a test run: the rows are received and counted and none is written.</param>
    public FlowLoadDestination(
        string name,
        PipelineLoadExecutor executor,
        string pipelineName,
        LoadUnit unit,
        LoadUnit? truncateFirst,
        IReadOnlyDictionary<string, object?> parameters,
        DateTimeOffset startedAt,
        ILogger? logger = null,
        bool skipWrites = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(parameters);
        _name = name;
        _executor = executor;
        _pipelineName = pipelineName;
        _unit = unit;
        _truncateFirst = truncateFirst;
        _parameters = parameters;
        _startedAt = startedAt;
        _logger = logger ?? NullLogger.Instance;
        _skipWrites = skipWrites;
    }

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [InputName];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [];

    /// <summary>Gets how many rows the load commands affected over the run.</summary>
    public int RowsLoaded { get; private set; }

    /// <summary>
    /// Whether a load must be given every row at once: one that states a soft delete flags the keys absent from its rows, and one that writes a file or files
    /// (FDW-3193) writes each file once, so a file is not added to again by a later batch and a bad value in a late row leaves no file.
    /// </summary>
    /// <param name="unit">The load.</param>
    /// <returns><see langword="true"/> when the load states a soft delete mode.</returns>
    internal static bool NeedsWholeSet(LoadUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return unit.Addressing is not null || !string.IsNullOrWhiteSpace(unit.Row.SoftDeleteMode);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var input = ports.Input(InputName);
        if (_skipWrites)
        {
            return await CountOnly(input, cancellationToken).ConfigureAwait(false);
        }

        var wholeSet = NeedsWholeSet(_unit);
        var held = new List<IDictionary<string, object?>>();
        var first = true;
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            if (batch.Count == 0)
            {
                continue;
            }

            if (wholeSet)
            {
                held.AddRange(batch.Rows);
                continue;
            }

            var loaded = await Load(batch.Rows, first, cancellationToken).ConfigureAwait(false);
            if (loaded.IsFailure)
            {
                return loaded;
            }

            first = false;
        }

        if (held.Count > 0)
        {
            var loaded = await Load(held, true, cancellationToken).ConfigureAwait(false);
            if (loaded.IsFailure)
            {
                return loaded;
            }
        }

        FlowLog.DestinationLoaded(_logger, _name, RowsLoaded);
        return GenericResult.Success();
    }

    // A destination that writes files empties each file it writes in the same unit of work as it writes it (FDW-3193): the files are known only from the
    // rows, so the truncate cannot run before them, and a file that fails to write is not left emptied.
    private async Task<IGenericResult> LoadFiles(IReadOnlyList<IDictionary<string, object?>> rows, bool first, CancellationToken cancellationToken)
    {
        IReadOnlyList<LoadUnit> units = first && _truncateFirst is not null ? [_truncateFirst, _unit] : [_unit];
        var loaded = await _executor.Run(_pipelineName, units, rows, _parameters, _startedAt, cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            return GenericResult.Failure(FlowLog.DestinationLoadFailed(_logger, _name, rows.Count, loaded.CurrentMessage));
        }

        RowsLoaded += loaded.Value;
        return GenericResult.Success();
    }

    // A test run: the rows are received and counted by the link, and none is written.
    private async Task<IGenericResult> CountOnly(IFlowInput input, CancellationToken cancellationToken)
    {
        var received = 0;
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            received += batch.Count;
        }

        FlowLog.DestinationSkippedWrites(_logger, _name, received);
        return GenericResult.Success();
    }

    // The truncate, when one is stated, runs once before the first rows are written, so a source that yields nothing never empties the
    // target.
    private async Task<IGenericResult> Load(IReadOnlyList<IDictionary<string, object?>> rows, bool first, CancellationToken cancellationToken)
    {
        if (_unit.Addressing is not null)
        {
            return await LoadFiles(rows, first, cancellationToken).ConfigureAwait(false);
        }

        if (first && _truncateFirst is not null)
        {
            var emptied = await _executor
                .Run(_pipelineName, [_truncateFirst], [], _parameters, _startedAt, cancellationToken)
                .ConfigureAwait(false);
            if (emptied.IsFailure)
            {
                return GenericResult.Failure(FlowLog.DestinationTruncateFailed(_logger, _name, emptied.CurrentMessage));
            }
        }

        var loaded = await _executor
            .Run(_pipelineName, [_unit], rows, _parameters, _startedAt, cancellationToken)
            .ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            return GenericResult.Failure(FlowLog.DestinationLoadFailed(_logger, _name, rows.Count, loaded.CurrentMessage));
        }

        RowsLoaded += loaded.Value;
        return GenericResult.Success();
    }
}
