using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Compilation;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations.Sets;

/// <summary>
/// Checks a calculation set and compiles every formula in it with the same engine that compiles a per-record calculation, so a
/// set and a record refuse the same mistakes against the same limits.
/// </summary>
public sealed class CalculationSetCompiler
{
    private readonly ILogger<CalculationSetCompiler> _logger;
    private readonly CalculationCompiler _formulas;

    /// <summary>Initializes a new instance of the <see cref="CalculationSetCompiler"/> class.</summary>
    /// <param name="logger">The logger; a compiler without one still works.</param>
    public CalculationSetCompiler(ILogger<CalculationSetCompiler>? logger = null)
    {
        _logger = logger ?? NullLogger<CalculationSetCompiler>.Instance;
        _formulas = new CalculationCompiler();
    }

    /// <summary>Checks and compiles a set.</summary>
    /// <param name="definition">The set.</param>
    /// <param name="limits">The limits on results, pieces and levels of any one formula.</param>
    /// <param name="setLimits">The limits on rows.</param>
    /// <returns>The compiled set, or the first coded refusal.</returns>
    public IGenericResult<ICompiledCalculationSet> Compile(CalculationSetDefinition definition, CalculationLimits limits, SetLimits setLimits)
    {
        CalculationSetLog.CompileStarted(_logger, definition.Name, definition.Outputs.Count);
        var shape = CalculationSetChecker.Check(definition);
        if (shape.IsFailure)
        {
            CalculationSetLog.CompileRefused(_logger, definition.Name, shape.Code?.Name);
            return shape.ToNewResult<ICompiledCalculationSet>();
        }

        var outputs = new List<CompiledOutput>(definition.Outputs.Count);
        var formulaCount = 0;
        foreach (var output in definition.Outputs)
        {
            var compiled = CompileOutput(definition, output, limits);
            if (compiled.IsFailure)
            {
                CalculationSetLog.CompileRefused(_logger, definition.Name, compiled.Code?.Name);
                return compiled.ToNewResult<ICompiledCalculationSet>();
            }

            outputs.Add(compiled.Value!);
            formulaCount += output.Rows.Formulas.Count + output.Columns.Count(column => column.Formula is not null);
        }

        CalculationSetLog.CompileCompleted(_logger, definition.Name, outputs.Count, formulaCount);
        return GenericResult<ICompiledCalculationSet>.Success(new CompiledCalculationSet(definition.Name, outputs, setLimits, _logger));
    }

    private IGenericResult<CompiledOutput> CompileOutput(CalculationSetDefinition definition, TableOutputDefinition output, CalculationLimits limits)
    {
        var sources = new List<ICompiledCalculation>(output.Rows.Formulas.Count);
        foreach (var formula in output.Rows.Formulas)
        {
            var compiled = _formulas.Compile(formula, limits);
            if (compiled.IsFailure)
            {
                return compiled.ToNewResult<CompiledOutput>();
            }

            sources.Add(compiled.Value!);
        }

        var columns = CompileColumns(output, limits);
        if (columns.IsFailure)
        {
            return columns.ToNewResult<CompiledOutput>();
        }

        return GenericResult<CompiledOutput>.Success(new CompiledOutput(output, sources, columns.Value, Needed(definition, output)));
    }

    private IGenericResult<ICompiledCalculation?> CompileColumns(TableOutputDefinition output, CalculationLimits limits)
    {
        var results = output.Columns
            .Where(column => column.Formula is not null)
            .Select(column => new CalculationResultDefinition(column.Name, column.Formula!, true))
            .ToList();
        if (results.Count == 0)
        {
            return GenericResult<ICompiledCalculation?>.Success(null);
        }

        var compiled = _formulas.Compile(new CalculationArrayDefinition(output.Name, results), limits);
        return compiled.IsFailure
            ? compiled.ToNewResult<ICompiledCalculation?>()
            : GenericResult<ICompiledCalculation?>.Success(compiled.Value);
    }

    // Only the totals and lookups an output reads are worked out for it: a lookup that finds nothing must not stop an output that never uses it.
    private static List<DerivedValueDefinition> Needed(CalculationSetDefinition definition, TableOutputDefinition output)
    {
        var read = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var column in output.Columns)
        {
            if (column.Formula is not null)
            {
                SetFormulaFields.Collect(column.Formula, read);
            }

            if (column.Reads is { } copied)
            {
                read.Add(copied);
            }
        }

        foreach (var formula in output.Rows.Formulas)
        {
            SetFormulaFields.Collect(formula, read);
        }

        return [.. definition.Derived.Where(value => read.Contains(value.Name))];
    }
}
