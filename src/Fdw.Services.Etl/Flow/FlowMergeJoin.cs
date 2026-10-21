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
/// A merge join: pairs the rows of two inputs on key fields and sends each pair out as one row (FDW-1386). SSIS Merge Join,
/// done as a hash join: neither input has to be sorted.
/// </summary>
/// <remarks>
/// <para>
/// The right input is built into a hash table by key; the left input then streams past it. The hash table holds the whole right
/// side, so put the smaller input on the right. Which unmatched rows are kept is stated by the <see cref="IJoinKind"/>.
/// </para>
/// <para>
/// A row whose key has a null never matches (as in SQL) but is kept or dropped as an unmatched row. The output row has the left
/// fields as they are and the right fields under <c>rightPrefix</c>; a collision between a left field and a prefixed right field
/// fails the run and names the field rather than overwriting one with the other.
/// </para>
/// </remarks>
public sealed class FlowMergeJoin : IFlowComponent
{
    /// <summary>The name of the left input node.</summary>
    public const string LeftName = "left";

    /// <summary>The name of the right input node.</summary>
    public const string RightName = "right";

    /// <summary>The name of the output node.</summary>
    public const string OutputName = "output";

    private readonly List<string> _leftKeys;
    private readonly List<string> _rightKeys;
    private readonly IJoinKind _kind;
    private readonly string _rightPrefix;
    private readonly int _batchSize;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowMergeJoin"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="leftKeys">The key fields of the left rows; at least one.</param>
    /// <param name="rightKeys">The key fields of the right rows, in the same order and number.</param>
    /// <param name="kind">Which unmatched rows are kept.</param>
    /// <param name="rightPrefix">What is put before each right field name in the output row; empty only when no name collides.</param>
    /// <param name="batchSize">How many rows go in an output batch; at least one.</param>
    /// <param name="logger">The logger.</param>
    public FlowMergeJoin(
        string name,
        IReadOnlyList<string> leftKeys,
        IReadOnlyList<string> rightKeys,
        IJoinKind kind,
        string rightPrefix,
        int batchSize,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(leftKeys);
        ArgumentNullException.ThrowIfNull(rightKeys);
        ArgumentNullException.ThrowIfNull(kind);
        if (ReferenceEquals(kind, JoinKinds.NotFound))
        {
            throw new ArgumentException("A join states which unmatched rows it keeps.", nameof(kind));
        }

        ArgumentNullException.ThrowIfNull(rightPrefix);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        if (leftKeys.Count == 0 || leftKeys.Count != rightKeys.Count)
        {
            throw new ArgumentException("A join states at least one key field and the same number on each side.", nameof(leftKeys));
        }

        Name = name;
        _leftKeys = [.. leftKeys];
        _rightKeys = [.. rightKeys];
        _kind = kind;
        _rightPrefix = rightPrefix;
        _batchSize = batchSize;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [LeftName, RightName];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [OutputName];

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var left = FlowInputPump.Start(ports.Input(LeftName), cancellationToken);
        var built = await BuildRight(ports.Input(RightName), cancellationToken).ConfigureAwait(false);
        if (built.IsFailure)
        {
            return built;
        }

        var output = new FlowBatchWriter(ports.Output(OutputName), _batchSize, cancellationToken);
        var leftFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var joined = await Probe(left, built.Value!, leftFields, output, cancellationToken).ConfigureAwait(false);
        await left.Completion.ConfigureAwait(false);
        if (joined.IsFailure)
        {
            return joined;
        }

        var tail = await UnmatchedRight(built.Value!, leftFields, output).ConfigureAwait(false);
        return tail.IsFailure ? tail : await output.Flush().ConfigureAwait(false);
    }

    private async Task<IGenericResult<RightSide>> BuildRight(IFlowInput input, CancellationToken cancellationToken)
    {
        var side = new RightSide();
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            foreach (var row in batch.Rows)
            {
                foreach (var field in row.Keys)
                {
                    side.Fields.Add(field);
                }

                if (!RecordKeyReader.TryRead(row, _rightKeys, out var key, out var missing))
                {
                    return GenericResult<RightSide>.Failure(FlowLog.FieldMissing(_logger, Name, RightName, missing!));
                }

                side.Add(new RightEntry(row), key);
            }
        }

        return GenericResult<RightSide>.Success(side);
    }

    private async Task<IGenericResult> Probe(
        FlowInputPump left, RightSide right, HashSet<string> leftFields, FlowBatchWriter output, CancellationToken cancellationToken)
    {
        await foreach (var batch in left.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var row in batch.Rows)
            {
                foreach (var field in row.Keys)
                {
                    leftFields.Add(field);
                }

                var sent = await SendLeft(row, right, output).ConfigureAwait(false);
                if (sent.IsFailure)
                {
                    return sent;
                }
            }
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> SendLeft(IDictionary<string, object?> row, RightSide right, FlowBatchWriter output)
    {
        if (!RecordKeyReader.TryRead(row, _leftKeys, out var key, out var missing))
        {
            return GenericResult.Failure(FlowLog.FieldMissing(_logger, Name, LeftName, missing!));
        }

        var partners = right.Find(key);
        if (partners.Count == 0)
        {
            return _kind.KeepsUnmatchedLeft
                ? await EmitPair(row, row.Keys, null, right.Fields, output).ConfigureAwait(false)
                : GenericResult.Success();
        }

        foreach (var partner in partners)
        {
            partner.Matched = true;
            var emitted = await EmitPair(row, row.Keys, partner.Row, right.Fields, output).ConfigureAwait(false);
            if (emitted.IsFailure)
            {
                return emitted;
            }
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> UnmatchedRight(RightSide right, HashSet<string> leftFields, FlowBatchWriter output)
    {
        if (!_kind.KeepsUnmatchedRight)
        {
            return GenericResult.Success();
        }

        foreach (var entry in right.Entries.Where(e => !e.Matched))
        {
            var emitted = await EmitPair(null, leftFields, entry.Row, right.Fields, output).ConfigureAwait(false);
            if (emitted.IsFailure)
            {
                return emitted;
            }
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> EmitPair(
        IDictionary<string, object?>? left,
        IEnumerable<string> leftNames,
        IDictionary<string, object?>? right,
        HashSet<string> rightNames,
        FlowBatchWriter output)
    {
        var built = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in leftNames)
        {
            built[field] = left is not null && left.TryGetValue(field, out var value) ? value : null;
        }

        foreach (var field in rightNames)
        {
            var target = _rightPrefix + field;
            if (built.ContainsKey(target))
            {
                return GenericResult.Failure(FlowLog.FieldCollision(_logger, Name, target));
            }

            built[target] = right is not null && right.TryGetValue(field, out var value) ? value : null;
        }

        await output.Add(built).ConfigureAwait(false);
        return GenericResult.Success();
    }

    private sealed class RightEntry(IDictionary<string, object?> row)
    {
        public IDictionary<string, object?> Row { get; } = row;

        public bool Matched { get; set; }
    }

    private sealed class RightSide
    {
        private readonly Dictionary<object?[], List<RightEntry>> _byKey = new(new KeyValuesComparer());

        public HashSet<string> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<RightEntry> Entries { get; } = [];

        public void Add(RightEntry entry, object?[] key)
        {
            Entries.Add(entry);
            if (key.Any(value => value is null))
            {
                return;
            }

            var normalised = KeyValuesComparer.Normalise(key);
            if (!_byKey.TryGetValue(normalised, out var list))
            {
                list = [];
                _byKey[normalised] = list;
            }

            list.Add(entry);
        }

        public List<RightEntry> Find(object?[] key) =>
            key.Any(value => value is null) || !_byKey.TryGetValue(KeyValuesComparer.Normalise(key), out var list) ? [] : list;
    }
}
