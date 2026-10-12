using System.Linq;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Turns the engine's tree back into the formula payload a request states, the reverse of <see cref="BuiltFormulaMapper"/>.</summary>
internal static class BuiltFormulaReader
{
    /// <summary>Maps one tree; the converter that produced it has already bounded its depth.</summary>
    /// <param name="node">The tree.</param>
    /// <returns>The payload.</returns>
    internal static BuiltFormulaNodePayload ToPayload(CalculationNode node) => new()
    {
        Op = node.Op,
        Name = node.Name,
        Value = node.Value,
        Flag = node.Flag,
        Places = node.Places,
        Mode = node.Mode,
        Operands = [.. node.Operands.Select(ToPayload)],
    };
}
