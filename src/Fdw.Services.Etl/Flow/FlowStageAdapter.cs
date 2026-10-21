using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Runs an existing pipeline stage kind as a flow component (FDW-1386): the adapter that keeps Filter, Calculate, Lookup, Aggregate,
/// Sort, Distinct, LatestBy, Validate and the rest usable behind input and output nodes until each is rewritten as a component.
/// </summary>
/// <remarks>
/// <para>
/// A stage reads one stream and writes one stream, so the adapter has one input node, <c>input</c>, and one output node,
/// <c>output</c>. A stage that streams is applied batch by batch. A stage that needs the whole input (<see cref="TransformTypeBase.RequiresCompleteInput"/>:
/// Sort, Aggregate, Distinct, LatestBy) is given every row once, when the input has ended; that is the one place the adapter holds rows.
/// </para>
/// <para>
/// A stage that rejects rows (a Validate that rejects) sends them to the component's error output, each with the component and the
/// reason added, as a map does. A component with no error output fails the run when its stage rejects a row; the rows are never
/// dropped without a word.
/// </para>
/// </remarks>
public sealed class FlowStageAdapter : IFlowComponent, IFlowErrorOutputs
{
    /// <summary>The name of the component's input node.</summary>
    public const string InputName = "input";

    /// <summary>The name of the component's output node.</summary>
    public const string OutputName = "output";

    private readonly TransformTypeBase _stage;
    private readonly IGenericConfiguration _configuration;
    private readonly TransformContext _context;
    private readonly int _batchSize;
    private readonly bool _wholeInput;
    private readonly string? _errorOutput;
    private int _rejectionsRouted;
    private int _errorsSeen;

    /// <summary>Initializes a new instance of the <see cref="FlowStageAdapter"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="stage">The stage kind to apply.</param>
    /// <param name="configuration">The stage's configuration.</param>
    /// <param name="context">The transform context the stage runs with.</param>
    /// <param name="batchSize">How many rows go in a batch when a whole-input stage emits its result; at least one. Stated only for a stage that holds every row; a stage that works a batch at a time has no use for it.</param>
    /// <param name="errorOutput">The name of an error output node that takes the rows the stage rejects; absent, a stage that rejects a row fails the run.</param>
    public FlowStageAdapter(string name, TransformTypeBase stage, IGenericConfiguration configuration, TransformContext context, int? batchSize, string? errorOutput = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // Why the answer is per configuration and not only per kind: a Compute stage works a row at a time, or on whole tables when
        // its transformation is a set, and only the compiled transformation says which.
        _wholeInput = stage.RequiresCompleteInput
            || (configuration is PipelineTransformConfiguration staged && context.BuiltSets.ContainsKey(staged.Id));
        if (_wholeInput)
        {
            if (batchSize is not { } given)
            {
                throw new ArgumentException("A stage that holds every row before it answers states how many rows go in a batch (batchSize).", nameof(batchSize));
            }

            if (given < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), given, "A batch has at least one row.");
            }

            _batchSize = given;
        }

        if (errorOutput is not null && string.Equals(errorOutput, OutputName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The error output cannot be named '{OutputName}'.", nameof(errorOutput));
        }

        _errorOutput = errorOutput;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> ErrorOutputs => _errorOutput is null ? [] : [_errorOutput];

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [InputName];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => _errorOutput is null ? [OutputName] : [OutputName, _errorOutput];

    /// <inheritdoc />
    public Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default) =>
        _wholeInput ? RunWhole(ports, cancellationToken) : RunStreaming(ports, cancellationToken);

    private async Task<IGenericResult> RunStreaming(IFlowPorts ports, CancellationToken cancellationToken)
    {
        var input = ports.Input(InputName);
        var output = ports.Output(OutputName);
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            var applied = await Apply(batch.Rows, cancellationToken).ConfigureAwait(false);
            if (applied.IsFailure)
            {
                return applied;
            }

            var routed = await RouteRejections(ports, cancellationToken).ConfigureAwait(false);
            if (routed.IsFailure)
            {
                return routed;
            }

            if (applied.Value!.Count > 0)
            {
                await output.Write(new FlowBatch(applied.Value), cancellationToken).ConfigureAwait(false);
            }
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> RunWhole(IFlowPorts ports, CancellationToken cancellationToken)
    {
        var input = ports.Input(InputName);
        var all = new List<IDictionary<string, object?>>();
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            all.AddRange(batch.Rows);
        }

        var applied = await Apply(all, cancellationToken).ConfigureAwait(false);
        if (applied.IsFailure)
        {
            return applied;
        }

        var routed = await RouteRejections(ports, cancellationToken).ConfigureAwait(false);
        if (routed.IsFailure)
        {
            return routed;
        }

        var output = ports.Output(OutputName);
        foreach (var page in applied.Value!.Chunk(_batchSize))
        {
            await output.Write(new FlowBatch(page), cancellationToken).ConfigureAwait(false);
        }

        return GenericResult.Success();
    }

    // Why the rows a stage dropped for an error of its own go the same way as the rows it rejected: a row that leaves the stream is
    // never lost without a word. It goes to the error output with the reason, or, with no error output to take it, the run fails.
    private async Task<IGenericResult> RouteRejections(IFlowPorts ports, CancellationToken cancellationToken)
    {
        var refusals = TakeRefusals();
        if (refusals.Count == 0)
        {
            return GenericResult.Success();
        }

        if (_errorOutput is not { } errorOutput || refusals.Any(refusal => refusal.Record is null))
        {
            return GenericResult.Failure(FlowLog.StageRejectedRows(_context.Logger, Name, refusals.Count, refusals[0].Reason));
        }

        var errors = new List<IDictionary<string, object?>>();
        foreach (var refusal in refusals)
        {
            if (refusal.Record is { } record)
            {
                errors.Add(FlowRowMapper.ErrorRow(record, Name, refusal.Reason));
            }
        }

        await ports.Output(errorOutput).Write(new FlowBatch(errors), cancellationToken).ConfigureAwait(false);
        return GenericResult.Success();
    }

    // A rejection is also reported as an error, so the errors that are not one of the new rejections are the stage's own errors:
    // a row it could not work on and left out.
    private List<Refusal> TakeRefusals()
    {
        var rejected = _context.Rejections.Skip(_rejectionsRouted).ToList();
        _rejectionsRouted = _context.Rejections.Count;
        var dropped = _context.Errors.Skip(_errorsSeen)
            .Where(error => !rejected.Any(rejection =>
                ReferenceEquals(rejection.Record, error.Record) && string.Equals(rejection.Reason, error.Message, StringComparison.Ordinal)))
            .ToList();
        _errorsSeen = _context.Errors.Count;
        return
        [
            .. rejected.Select(rejection => new Refusal(rejection.Record, rejection.Reason)),
            .. dropped.Select(error => new Refusal(error.Record, error.Message)),
        ];
    }

    private sealed record Refusal(IDictionary<string, object?>? Record, string Reason);

    private async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> Apply(
        IReadOnlyList<IDictionary<string, object?>> rows, CancellationToken cancellationToken)
    {
        var transformed = await _stage.TransformBatch(rows, _configuration, _context, cancellationToken).ConfigureAwait(false);
        return transformed.IsFailure
            ? transformed.ToNewResult<IReadOnlyList<IDictionary<string, object?>>>()
            : GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success([.. transformed.Value!]);
    }
}
