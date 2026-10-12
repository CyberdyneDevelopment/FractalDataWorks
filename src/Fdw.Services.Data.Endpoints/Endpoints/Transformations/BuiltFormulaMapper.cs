using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Turns the formula a request states into the engine's tree, refusing one nested deeper than the limit before it is walked
/// any further, so a request cannot ask for a walk the limit forbids.
/// </summary>
internal static class BuiltFormulaMapper
{
    /// <summary>Maps one result's formula.</summary>
    /// <param name="resultName">The result the formula belongs to; it places a refusal.</param>
    /// <param name="formula">The formula as the request states it.</param>
    /// <param name="maxDepth">How many levels a formula may have.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The tree, or the refusal.</returns>
    internal static IGenericResult<CalculationNode> ToNode(string resultName, BuiltFormulaNodePayload? formula, int maxDepth, ILogger logger) =>
        formula is null
            ? GenericResult<CalculationNode>.Failure(BuiltTransformationEndpointLog.FormulaMissing(logger, resultName))
            : Map(resultName, formula, 1, maxDepth, logger);

    private static IGenericResult<CalculationNode> Map(string resultName, BuiltFormulaNodePayload node, int depth, int maxDepth, ILogger logger)
    {
        if (depth > maxDepth)
        {
            return GenericResult<CalculationNode>.Failure(BuiltTransformationEndpointLog.FormulaTooDeep(logger, resultName, maxDepth));
        }

        if (node.Operands is null)
        {
            return GenericResult<CalculationNode>.Failure(BuiltTransformationEndpointLog.OperandsMissing(logger, resultName));
        }

        var operands = new List<CalculationNode>(node.Operands.Count);
        foreach (var operand in node.Operands)
        {
            var mapped = operand is null
                ? GenericResult<CalculationNode>.Failure(BuiltTransformationEndpointLog.OperandsMissing(logger, resultName))
                : Map(resultName, operand, depth + 1, maxDepth, logger);
            if (mapped.IsFailure)
            {
                return mapped;
            }

            operands.Add(mapped.Value!);
        }

        return GenericResult<CalculationNode>.Success(
            new CalculationNode(node.Op, node.Name, node.Value, node.Flag, node.Places, node.Mode, operands));
    }
}
