using System;
using System.Collections.Generic;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Finds the fields a formula reads, which must each be a declared input of the transformation.</summary>
internal static class BuiltFormulaFields
{
    private const string FieldOperator = "Field";

    /// <summary>Collects the names a tree's Field pieces read, in the order they are met.</summary>
    /// <param name="node">The root of a formula already checked against the depth limit.</param>
    /// <param name="found">The names read so far; this tree's are added.</param>
    internal static void Collect(CalculationNode node, List<string> found)
    {
        if (string.Equals(node.Op, FieldOperator, StringComparison.Ordinal) && node.Name is { } name)
        {
            found.Add(name);
        }

        foreach (var operand in node.Operands)
        {
            Collect(operand, found);
        }
    }
}
