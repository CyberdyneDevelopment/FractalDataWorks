using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations.Compilation;

/// <summary>
/// Compiles a calculation array, or a single chain, into one delegate built from expression trees.
/// </summary>
/// <remarks>
/// Why compile: a settlement run evaluates about a hundred results over tens of thousands of records, and walking an option
/// per node per record, boxing each value and building a dictionary of parameters at every step, costs more than the
/// arithmetic. The compiled delegate does the arithmetic and nothing else. Compile an array once per run and share the
/// result; compiling per record would cost more than it saves. The tree is validated first, so what reaches the compiler is
/// known to be well formed and a refusal is always a coded result.
/// </remarks>
public sealed class CalculationCompiler
{
    private readonly ILogger<CalculationCompiler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationCompiler"/> class.
    /// </summary>
    /// <param name="logger">The logger; optional.</param>
    public CalculationCompiler(ILogger<CalculationCompiler>? logger = null)
    {
        _logger = logger ?? NullLogger<CalculationCompiler>.Instance;
    }

    /// <summary>Compiles one chain as an array with a single result named "result".</summary>
    /// <param name="chain">The tree.</param>
    /// <param name="limits">The stated limits.</param>
    /// <returns>The compiled calculation, or the coded refusal.</returns>
    public IGenericResult<CompiledCalculation> Compile(CalculationNode chain, CalculationLimits limits) =>
        Compile(new CalculationArrayDefinition("chain", new[] { new CalculationResultDefinition("result", chain, true) }), limits);

    /// <summary>Compiles an array.</summary>
    /// <param name="array">The array.</param>
    /// <param name="limits">The stated limits.</param>
    /// <returns>The compiled calculation, or the coded refusal.</returns>
    public IGenericResult<CompiledCalculation> Compile(CalculationArrayDefinition array, CalculationLimits limits)
    {
        CalculationCompilerLog.CompileStarted(_logger, array.Name, array.Results.Count);
        var validation = CalculationArrayValidator.Validate(array, limits);
        if (validation.IsFailure)
        {
            CalculationCompilerLog.CompileRefused(_logger, array.Name, validation.Code?.Name);
            return validation.ToNewResult<CompiledCalculation>();
        }

        try
        {
            return GenericResult<CompiledCalculation>.Success(Build(array));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            CalculationCompilerLog.CompileRefused(_logger, array.Name, "OperandTypeMismatch");
            return GenericResult<CompiledCalculation>.Failure(
                CalculationTreeResultCodes.ByName("OperandTypeMismatch"),
                ResultDetails.Create("node", array.Name, "reason", exception.Message));
        }
    }

    private CompiledCalculation Build(CalculationArrayDefinition array)
    {
        var unit = new CalculationCompileUnit();
        var statements = new List<Expression>(array.Results.Count);
        var names = new List<string>(array.Results.Count);
        var emitted = new List<string>(array.Results.Count);
        foreach (var result in array.Results)
        {
            statements.Add(unit.Statement(result, names.Count, result.Emit ? emitted.Count : -1));
            names.Add(result.Name);
            if (result.Emit)
            {
                emitted.Add(result.Name);
            }
        }

        var compiled = new CompiledCalculation(unit.Compile(statements), emitted, names, unit.Labels);
        CalculationCompilerLog.CompileCompleted(_logger, array.Name, names.Count, emitted.Count, unit.Labels.Count);
        return compiled;
    }
}
