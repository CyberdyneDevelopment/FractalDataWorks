using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A lookup: brings columns from a reference input onto each row of the main input, by key (FDW-1386). SSIS Lookup, with the
/// reference as a second input node rather than a query of its own.
/// </summary>
/// <remarks>
/// <para>
/// The reference input is read into a hash table first; the main input then streams past it. A row with a match leaves through
/// <c>output</c> with the brought columns added. A row with no match goes to the unmatched output the <see cref="FlowUnmatched"/>
/// names, or fails the run; it is never sent on with nulls in the columns it was supposed to get.
/// </para>
/// <para>
/// Why duplicate reference keys fail: which of two reference rows a row should receive is a choice the lookup would be making
/// for the author. The reference is made unique first (a Distinct or Latest-by component) or the lookup is not the right component.
/// </para>
/// </remarks>
public sealed class FlowLookup : IFlowComponent
{
    /// <summary>The name of the main input node.</summary>
    public const string InputName = "input";

    /// <summary>The name of the reference input node.</summary>
    public const string ReferenceName = "reference";

    /// <summary>The name of the matched output node.</summary>
    public const string OutputName = "output";

    private readonly List<string> _keys;
    private readonly List<string> _referenceKeys;
    private readonly IReadOnlyList<FlowFieldMap> _bring;
    private readonly FlowUnmatched _unmatched;
    private readonly int _batchSize;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowLookup"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="keys">The key fields of the main rows; at least one.</param>
    /// <param name="referenceKeys">The key fields of the reference rows, in the same order and number.</param>
    /// <param name="bring">The columns brought across: each target field from a field of the reference row; at least one.</param>
    /// <param name="unmatched">What happens to a row with no reference row.</param>
    /// <param name="batchSize">How many rows go in an output batch; at least one.</param>
    /// <param name="logger">The logger.</param>
    public FlowLookup(
        string name,
        IReadOnlyList<string> keys,
        IReadOnlyList<string> referenceKeys,
        IReadOnlyList<FlowFieldMap> bring,
        FlowUnmatched unmatched,
        int batchSize,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(referenceKeys);
        ArgumentNullException.ThrowIfNull(bring);
        ArgumentNullException.ThrowIfNull(unmatched);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        if (keys.Count == 0 || keys.Count != referenceKeys.Count)
        {
            throw new ArgumentException("A lookup states at least one key field and the same number on each side.", nameof(keys));
        }

        if (bring.Count == 0 || bring.Any(map => map.IsConstant))
        {
            throw new ArgumentException("A lookup brings at least one column, each from a field of the reference row.", nameof(bring));
        }

        if (unmatched.Output is not null && string.Equals(unmatched.Output, OutputName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The unmatched output cannot be named '{OutputName}'.", nameof(unmatched));
        }

        Name = name;
        _keys = [.. keys];
        _referenceKeys = [.. referenceKeys];
        _bring = bring;
        _unmatched = unmatched;
        _batchSize = batchSize;
        Outputs = unmatched.Output is null ? [OutputName] : [OutputName, unmatched.Output];
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [InputName, ReferenceName];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var main = FlowInputPump.Start(ports.Input(InputName), cancellationToken);
        var table = await BuildReference(ports.Input(ReferenceName), cancellationToken).ConfigureAwait(false);
        if (table.IsFailure)
        {
            return table;
        }

        var matched = new FlowBatchWriter(ports.Output(OutputName), _batchSize, cancellationToken);
        var unmatched = _unmatched.Output is null
            ? null
            : new FlowBatchWriter(ports.Output(_unmatched.Output), _batchSize, cancellationToken);
        var looked = await Stream(main, table.Value!, matched, unmatched, cancellationToken).ConfigureAwait(false);
        await main.Completion.ConfigureAwait(false);
        if (looked.IsFailure)
        {
            return looked;
        }

        if (unmatched is not null)
        {
            await unmatched.Flush().ConfigureAwait(false);
        }

        return await matched.Flush().ConfigureAwait(false);
    }

    private async Task<IGenericResult<Dictionary<object?[], IDictionary<string, object?>>>> BuildReference(
        IFlowInput input, CancellationToken cancellationToken)
    {
        var table = new Dictionary<object?[], IDictionary<string, object?>>(new KeyValuesComparer());
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            foreach (var row in batch.Rows)
            {
                if (!RecordKeyReader.TryRead(row, _referenceKeys, out var key, out var missing))
                {
                    return GenericResult<Dictionary<object?[], IDictionary<string, object?>>>.Failure(
                        FlowLog.FieldMissing(_logger, Name, ReferenceName, missing!));
                }

                if (key.Any(value => value is null))
                {
                    continue;
                }

                if (!table.TryAdd(KeyValuesComparer.Normalise(key), row))
                {
                    return GenericResult<Dictionary<object?[], IDictionary<string, object?>>>.Failure(
                        FlowLog.DuplicateReferenceKey(_logger, Name, string.Join(", ", key)));
                }
            }
        }

        return GenericResult<Dictionary<object?[], IDictionary<string, object?>>>.Success(table);
    }

    private async Task<IGenericResult> Stream(
        FlowInputPump main,
        Dictionary<object?[], IDictionary<string, object?>> table,
        FlowBatchWriter matched,
        FlowBatchWriter? unmatched,
        CancellationToken cancellationToken)
    {
        await foreach (var batch in main.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var row in batch.Rows)
            {
                var sent = await SendRow(row, table, matched, unmatched).ConfigureAwait(false);
                if (sent.IsFailure)
                {
                    return sent;
                }
            }
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> SendRow(
        IDictionary<string, object?> row,
        Dictionary<object?[], IDictionary<string, object?>> table,
        FlowBatchWriter matched,
        FlowBatchWriter? unmatched)
    {
        if (!RecordKeyReader.TryRead(row, _keys, out var key, out var missing))
        {
            return GenericResult.Failure(FlowLog.FieldMissing(_logger, Name, InputName, missing!));
        }

        if (key.Any(value => value is null) || !table.TryGetValue(KeyValuesComparer.Normalise(key), out var reference))
        {
            if (unmatched is null)
            {
                return GenericResult.Failure(FlowLog.LookupNoMatch(_logger, Name, string.Join(", ", key)));
            }

            await unmatched.Add(row).ConfigureAwait(false);
            return GenericResult.Success();
        }

        var built = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);
        foreach (var map in _bring)
        {
            if (built.ContainsKey(map.Target))
            {
                return GenericResult.Failure(FlowLog.FieldCollision(_logger, Name, map.Target));
            }

            if (!reference.TryGetValue(map.Source!, out var value))
            {
                return GenericResult.Failure(FlowLog.FieldMissing(_logger, Name, ReferenceName, map.Source!));
            }

            built[map.Target] = value;
        }

        await matched.Add(built).ConfigureAwait(false);
        return GenericResult.Success();
    }
}
