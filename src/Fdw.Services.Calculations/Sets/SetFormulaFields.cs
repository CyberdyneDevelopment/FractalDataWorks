using System;
using System.Collections.Generic;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Sets;

/// <summary>Finds the names a formula reads, so only the totals and lookups an output uses are worked out for it.</summary>
internal static class SetFormulaFields
{
    private const string FieldOperator = "Field";

    internal static void Collect(CalculationNode node, HashSet<string> names)
    {
        if (string.Equals(node.Op, FieldOperator, StringComparison.Ordinal) && node.Name is { } name)
        {
            names.Add(name);
        }

        foreach (var operand in node.Operands)
        {
            Collect(operand, names);
        }
    }
}
