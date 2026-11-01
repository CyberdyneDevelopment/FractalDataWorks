using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Checks that what an output's formulas and copies read is something that output can see: the single inputs, the derived
/// values it uses, and the columns of its row source. A name it cannot see would fail every run, so it is refused when the
/// transformation is saved.
/// </summary>
internal static class BuiltSetVisibility
{
    internal static IGenericResult Check(CalculationSetDefinition definition, IEnumerable<string> scalars, ILogger logger)
    {
        var singles = scalars.Concat(definition.Derived.Select(value => value.Name)).ToList();
        foreach (var output in definition.Outputs)
        {
            var visible = new HashSet<string>(singles.Concat(Columns(definition, output)), StringComparer.Ordinal);
            var problem = FirstUnseen(output, visible);
            if (problem is { } found)
            {
                return GenericResult.Failure(BuiltTransformationEndpointLog.NameNotVisible(logger, output.Name, found.Column, found.Name));
            }
        }

        return GenericResult.Success();
    }

    // The columns a row of this output can read: those of the table it is made from, or the group columns, and the position of a count.
    private static IEnumerable<string> Columns(CalculationSetDefinition definition, TableOutputDefinition output)
    {
        var table = definition.TableInputs.FirstOrDefault(candidate => string.Equals(candidate.Name, output.Rows.TableName, StringComparison.Ordinal));
        IEnumerable<string> own = output.Rows.GroupBy.Count > 0
            ? output.Rows.GroupBy
            : table?.Columns.Select(column => column.Name) ?? [];
        return output.Rows.IndexColumn is { } index ? own.Append(index) : own;
    }

    private static (string Column, string Name)? FirstUnseen(TableOutputDefinition output, HashSet<string> visible)
    {
        foreach (var formula in output.Rows.Formulas)
        {
            var unseen = Unseen(formula, visible);
            if (unseen is not null)
            {
                return ("(row source)", unseen);
            }
        }

        foreach (var column in output.Columns)
        {
            var unseen = column.Formula is { } formula ? Unseen(formula, visible) : null;
            unseen ??= column.Reads is { } copied && !visible.Contains(copied) ? copied : null;
            if (unseen is not null)
            {
                return (column.Name, unseen);
            }
        }

        return null;
    }

    private static string? Unseen(CalculationNode formula, HashSet<string> visible)
    {
        var read = new List<string>();
        BuiltFormulaFields.Collect(formula, read);
        return read.FirstOrDefault(name => !visible.Contains(name));
    }
}
