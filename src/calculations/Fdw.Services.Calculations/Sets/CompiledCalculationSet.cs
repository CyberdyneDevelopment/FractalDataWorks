using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Sets;

/// <summary>A calculation set compiled once. It reads tables as name-to-value rows and writes tables the same way, and knows nothing about where either lives.</summary>
public sealed class CompiledCalculationSet : ICompiledCalculationSet
{
    private readonly string _name;
    private readonly IReadOnlyList<CompiledOutput> _outputs;
    private readonly SetLimits _limits;
    private readonly ILogger _logger;

    internal CompiledCalculationSet(string name, IReadOnlyList<CompiledOutput> outputs, SetLimits limits, ILogger logger)
    {
        _name = name;
        _outputs = outputs;
        _limits = limits;
        _logger = logger;
        OutputNames = [.. outputs.Select(output => output.Definition.Name)];
    }

    /// <inheritdoc />
    public IReadOnlyList<string> OutputNames { get; }

    /// <inheritdoc />
    public IGenericResult<CalculationSetResult> Evaluate(
        IReadOnlyDictionary<string, object?> scalars,
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> tables) =>
        Evaluate(scalars, tables, static _ => false);

    /// <inheritdoc />
    public IGenericResult<CalculationSetResult> Evaluate(
        IReadOnlyDictionary<string, object?> scalars,
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> tables,
        SetRowFailureHandler onRowFailure)
    {
        var scope = new SetRunScope(scalars, tables, _limits);
        var written = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>(_outputs.Count, System.StringComparer.Ordinal);
        foreach (var output in _outputs)
        {
            var rows = output.Evaluate(scope, onRowFailure);
            if (rows.IsFailure)
            {
                return rows.ToNewResult<CalculationSetResult>();
            }

            written[output.Definition.Name] = rows.Value!;
        }

        CalculationSetLog.Evaluated(_logger, _name, written.Values.Sum(rows => rows.Count), written.Count);
        return GenericResult<CalculationSetResult>.Success(new CalculationSetResult(written));
    }
}
