using System;
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
/// A conditional split: sends each row out through the output nodes whose filters it meets (FDW-1386). SSIS Conditional Split, with
/// the routing stated.
/// </summary>
/// <remarks>
/// It has one input node, <c>input</c>, and one output node per filter. A row that meets none is sent to the output named by the
/// unmatched rule or fails the run, as stated; it is never dropped. The outputs of a split are not rejoined: link them where
/// the rows should go.
/// </remarks>
public sealed class FlowConditionalSplit : IFlowComponent
{
    /// <summary>The name of the component's input node.</summary>
    public const string InputName = "input";

    private readonly IReadOnlyList<FlowSplitOutput> _outputs;
    private readonly ISplitRouting _routing;
    private readonly FlowUnmatched _unmatched;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowConditionalSplit"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="outputs">The filtered outputs, in the order they are tried; at least one.</param>
    /// <param name="routing">What happens to a row that meets more than one filter.</param>
    /// <param name="unmatched">What happens to a row that meets none.</param>
    /// <param name="logger">The logger.</param>
    public FlowConditionalSplit(
        string name,
        IReadOnlyList<FlowSplitOutput> outputs,
        ISplitRouting routing,
        FlowUnmatched unmatched,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentNullException.ThrowIfNull(routing);
        ArgumentNullException.ThrowIfNull(unmatched);
        if (ReferenceEquals(routing, SplitRoutings.NotFound))
        {
            throw new ArgumentException("A split states where a row that meets several filters goes.", nameof(routing));
        }
        if (outputs.Count == 0)
        {
            throw new ArgumentException("A split states at least one filtered output.", nameof(outputs));
        }

        var names = outputs.Select(o => o.Name).ToList();
        if (unmatched.Output is not null)
        {
            names.Add(unmatched.Output);
        }

        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
        {
            throw new ArgumentException("A split's output names, the unmatched output included, are all different.", nameof(outputs));
        }

        Name = name;
        _outputs = outputs;
        _routing = routing;
        _unmatched = unmatched;
        _logger = logger ?? NullLogger.Instance;
        Outputs = names;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [InputName];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var input = ports.Input(InputName);
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            var routed = Route(batch);
            if (routed.IsFailure)
            {
                return routed;
            }

            foreach (var (output, rows) in routed.Value!.Where(pair => pair.Value.Count > 0))
            {
                await ports.Output(output).Write(new FlowBatch(rows), cancellationToken).ConfigureAwait(false);
            }
        }

        return GenericResult.Success();
    }

    private IGenericResult<Dictionary<string, List<IDictionary<string, object?>>>> Route(FlowBatch batch)
    {
        var byOutput = Outputs.ToDictionary(o => o, _ => new List<IDictionary<string, object?>>(), StringComparer.OrdinalIgnoreCase);
        foreach (var row in batch.Rows)
        {
            var matched = false;
            foreach (var output in _outputs)
            {
                if (!output.Filter(row))
                {
                    continue;
                }

                matched = true;
                byOutput[output.Name].Add(row);
                if (!_routing.SendsToEveryMatch)
                {
                    break;
                }
            }

            if (matched)
            {
                continue;
            }

            if (_unmatched.Output is null)
            {
                return GenericResult<Dictionary<string, List<IDictionary<string, object?>>>>.Failure(
                    FlowLog.RowMatchedNoOutput(_logger, Name));
            }

            byOutput[_unmatched.Output].Add(row);
        }

        return GenericResult<Dictionary<string, List<IDictionary<string, object?>>>>.Success(byOutput);
    }
}
