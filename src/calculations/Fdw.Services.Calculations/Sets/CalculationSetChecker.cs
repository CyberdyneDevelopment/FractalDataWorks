using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;

namespace Fdw.Services.Calculations.Sets;

/// <summary>Checks the shape of a calculation set before any formula is compiled: names are present and unique, and every table named exists.</summary>
internal static class CalculationSetChecker
{
    internal static IGenericResult Check(CalculationSetDefinition definition)
    {
        var tables = definition.TableInputs.Select(table => table.Name).ToList();
        var problem = Duplicate("table input", tables)
            ?? Duplicate("derived value", definition.Derived.Select(value => value.Name))
            ?? Duplicate("output", definition.Outputs.Select(output => output.Name))
            ?? DerivedProblem(definition, tables)
            ?? OutputsProblem(definition, tables);
        return problem is null ? GenericResult.Success() : SetFailure.Of("SetDefinitionInvalid", definition.Name, problem);
    }

    private static string? Duplicate(string kind, IEnumerable<string> names)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return $"a {kind} has a blank name";
            }

            if (!seen.Add(name))
            {
                return $"{kind} '{name}' is declared twice";
            }
        }

        return null;
    }

    private static string? DerivedProblem(CalculationSetDefinition definition, List<string> tables)
    {
        var missing = definition.Derived.FirstOrDefault(value => !tables.Contains(value.Table, StringComparer.Ordinal));
        return missing is null ? null : $"derived value '{missing.Name}' reads table '{missing.Table}', which is not a table input";
    }

    private static string? OutputsProblem(CalculationSetDefinition definition, List<string> tables)
    {
        foreach (var output in definition.Outputs)
        {
            var problem = Duplicate($"column of output '{output.Name}'", output.Columns.Select(column => column.Name))
                ?? (output.Columns.Count == 0 ? $"output '{output.Name}' has no columns" : null)
                ?? ShadowProblem(definition, output)
                ?? (output.Rows.TableName is { } table && !tables.Contains(table, StringComparer.Ordinal)
                    ? $"output '{output.Name}' reads table '{table}', which is not a table input"
                    : null);
            if (problem is not null)
            {
                return problem;
            }
        }

        return null;
    }

    private static string? ShadowProblem(CalculationSetDefinition definition, TableOutputDefinition output)
    {
        var shadowed = output.Columns.FirstOrDefault(column =>
            column.Formula is not null && definition.Derived.Any(value => string.Equals(value.Name, column.Name, StringComparison.OrdinalIgnoreCase)));
        return shadowed is null ? null : $"formula column '{shadowed.Name}' of output '{output.Name}' has the name of a derived value (names are compared ignoring case)";
    }
}
