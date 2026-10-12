using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Data.Transformations;

/// <summary>
/// Converts a Built transformation between its stored rows and the calculation engine's array of result trees, in both
/// directions.
/// </summary>
/// <remarks>
/// Why rows and not text: a formula is stored as one row per piece, so it can be queried, validated by the database's
/// own keys and edited piece by piece. The engine's tree is the shape the compiler reads; this class is the only place the
/// two meet. Reading refuses, with a coded result, rows that do not form exactly one tree per output; it does not repair
/// them.
/// </remarks>
public static class BuiltTransformationConverter
{
    /// <summary>Reads the stored outputs and formula rows into the engine's array.</summary>
    /// <param name="built">The Built transformation as stored.</param>
    /// <param name="limits">The stated limits; the depth limit bounds how far a stored tree is followed.</param>
    /// <returns>The array, or the coded refusal naming the output and what is wrong with its rows.</returns>
    public static IGenericResult<CalculationArrayDefinition> ToArray(BuiltTransformationConfiguration built, CalculationLimits limits)
    {
        var results = new List<CalculationResultDefinition>(built.Outputs.Count);
        foreach (var output in built.Outputs.OrderBy(candidate => candidate.ExecutionOrder))
        {
            var pieces = built.Nodes
                .Where(row => string.Equals(row.OutputName, output.Name, StringComparison.Ordinal))
                .Cast<IBuiltFormulaNodeRow>()
                .ToList();
            var tree = BuiltFormulaTrees.Read(built.Name, output.Name, pieces, limits.MaxDepth);
            if (tree.IsFailure)
            {
                return tree.ToNewResult<CalculationArrayDefinition>();
            }

            results.Add(new CalculationResultDefinition(output.Name, tree.Value!, output.IsKept));
        }

        return GenericResult<CalculationArrayDefinition>.Success(new CalculationArrayDefinition(built.Name, results));
    }

    /// <summary>Writes an array of result trees as the stored outputs and formula rows of a Built transformation.</summary>
    /// <param name="name">The transformation's name.</param>
    /// <param name="array">The engine's array.</param>
    /// <param name="inputs">The declared inputs; they are kept as given.</param>
    /// <returns>The Built transformation, ready to save.</returns>
    public static BuiltTransformationConfiguration ToConfiguration(
        string name,
        CalculationArrayDefinition array,
        IReadOnlyList<BuiltTransformationInputConfiguration> inputs)
    {
        var built = new BuiltTransformationConfiguration
        {
            Name = name,
            Implementation = BuiltTransformationConfiguration.ImplementationName,
        };
        foreach (var input in inputs)
        {
            built.Inputs.Add(input);
        }

        for (var order = 0; order < array.Results.Count; order++)
        {
            var result = array.Results[order];
            built.Outputs.Add(new BuiltTransformationOutputConfiguration { Name = result.Name, IsKept = result.Emit, ExecutionOrder = order });
            foreach (var row in BuiltFormulaTrees.Flatten(result.Expression, () => new BuiltTransformationOutputNodeConfiguration { OutputName = result.Name }))
            {
                built.Nodes.Add(row);
            }
        }

        return built;
    }
}
