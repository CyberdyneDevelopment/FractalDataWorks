using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A cross apply: turns one row into one row per element of a list field (FDW-1386).
/// </summary>
/// <remarks>
/// <para>
/// The field holds a list (an array, a collection) or, when a delimiter is stated, delimited text. Each element becomes a copy
/// of the row with the element under the target field. A row whose list is empty or null produces no rows (inner) or one row with
/// the target null (outer), as stated.
/// </para>
/// <para>
/// Text with no delimiter stated fails the run: whether "a,b" is one element or two is the author's to say.
/// </para>
/// </remarks>
public sealed class FlowCrossApply : IFlowComponent
{
    /// <summary>The name of the input node.</summary>
    public const string InputName = "input";

    /// <summary>The name of the output node.</summary>
    public const string OutputName = "output";

    private readonly string _field;
    private readonly string _target;
    private readonly string? _delimiter;
    private readonly bool _keepsEmpty;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowCrossApply"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="field">The field that holds the list.</param>
    /// <param name="target">The field that receives each element.</param>
    /// <param name="delimiter">The delimiter text is split on; null when the field holds lists only.</param>
    /// <param name="keepsEmpty">Whether a row with no elements is sent on once with the target null (outer apply).</param>
    /// <param name="logger">The logger.</param>
    public FlowCrossApply(string name, string field, string target, string? delimiter, bool keepsEmpty, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        if (delimiter is { Length: 0 })
        {
            throw new ArgumentException("A delimiter is at least one character; state null for lists only.", nameof(delimiter));
        }

        Name = name;
        _field = field;
        _target = target;
        _delimiter = delimiter;
        _keepsEmpty = keepsEmpty;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [InputName];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [OutputName];

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var input = ports.Input(InputName);
        var output = ports.Output(OutputName);
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            var expanded = Expand(batch);
            if (expanded.IsFailure)
            {
                return expanded;
            }

            if (expanded.Value!.Count > 0)
            {
                await output.Write(expanded.Value, cancellationToken).ConfigureAwait(false);
            }
        }

        return GenericResult.Success();
    }

    private IGenericResult<FlowBatch> Expand(FlowBatch batch)
    {
        var rows = new List<IDictionary<string, object?>>(batch.Count);
        foreach (var row in batch.Rows)
        {
            if (!row.TryGetValue(_field, out var value))
            {
                return GenericResult<FlowBatch>.Failure(FlowLog.FieldMissing(_logger, Name, InputName, _field));
            }

            var elements = Elements(value);
            if (elements is null)
            {
                return GenericResult<FlowBatch>.Failure(FlowLog.TextNeedsDelimiter(_logger, Name, _field));
            }

            var any = false;
            foreach (var element in elements)
            {
                any = true;
                rows.Add(WithElement(row, element));
            }

            if (!any && _keepsEmpty)
            {
                rows.Add(WithElement(row, null));
            }
        }

        return GenericResult<FlowBatch>.Success(new FlowBatch(rows));
    }

    // Null when the value is text and no delimiter was stated.
    private IEnumerable<object?>? Elements(object? value)
    {
        if (value is null)
        {
            return Array.Empty<object?>();
        }

        if (value is string text)
        {
            if (_delimiter is null)
            {
                return null;
            }

            return text.Length == 0
                ? Array.Empty<object?>()
                : text.Split(_delimiter, StringSplitOptions.None).Cast<object?>();
        }

        return value is IEnumerable list ? list.Cast<object?>() : [value];
    }

    private Dictionary<string, object?> WithElement(IDictionary<string, object?> row, object? element) =>
        new(row, StringComparer.OrdinalIgnoreCase)
        {
            [_target] = element
        };
}
