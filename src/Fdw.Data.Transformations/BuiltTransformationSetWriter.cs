using System;
using System.Globalization;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Data.Transformations;

/// <summary>
/// Writes a calculation set definition as the stored rows of a Built transformation: table inputs and their columns, derived
/// values, and table outputs with their group columns, columns and formula pieces. Nothing is stored as text but the stated
/// value of a literal column.
/// </summary>
public static class BuiltTransformationSetWriter
{
    private const string ColumnRole = "Column";
    private const string WhereRole = "Where";
    private const string CountRole = "Count";

    /// <summary>Adds the rows of a set to a Built transformation, after the rows it already has.</summary>
    /// <param name="built">The Built transformation to fill.</param>
    /// <param name="definition">The set.</param>
    /// <returns>Success, or the refusal naming the literal that cannot be stored.</returns>
    public static IGenericResult Apply(BuiltTransformationConfiguration built, CalculationSetDefinition definition)
    {
        WriteTables(built, definition);
        WriteDerived(built, definition);
        for (var order = 0; order < definition.Outputs.Count; order++)
        {
            var written = WriteOutput(built, definition.Outputs[order], order, definition.Name);
            if (written.IsFailure)
            {
                return written;
            }
        }

        return GenericResult.Success();
    }

    private static void WriteTables(BuiltTransformationConfiguration built, CalculationSetDefinition definition)
    {
        for (var order = 0; order < definition.TableInputs.Count; order++)
        {
            var table = definition.TableInputs[order];
            built.TableInputs.Add(new BuiltTransformationTableInputConfiguration { Name = table.Name, ExecutionOrder = order });
            for (var position = 0; position < table.Columns.Count; position++)
            {
                built.TableInputColumns.Add(new BuiltTransformationTableInputColumnConfiguration
                {
                    TableName = table.Name,
                    Name = table.Columns[position].Name,
                    ValueType = table.Columns[position].ValueType,
                    ExecutionOrder = position,
                });
            }
        }
    }

    private static void WriteDerived(BuiltTransformationConfiguration built, CalculationSetDefinition definition)
    {
        for (var order = 0; order < definition.Derived.Count; order++)
        {
            var value = definition.Derived[order];
            built.DerivedValues.Add(new BuiltTransformationDerivedConfiguration
            {
                Name = value.Name,
                Kind = value.Kind,
                TableName = value.Table,
                ColumnName = value.Column,
                KeyColumn = value.KeyColumn,
                MatchField = value.MatchField,
                ExecutionOrder = order,
            });
        }
    }

    private static IGenericResult WriteOutput(BuiltTransformationConfiguration built, TableOutputDefinition output, int order, string set)
    {
        built.TableOutputs.Add(new BuiltTransformationTableOutputConfiguration
        {
            Name = output.Name,
            RowSource = output.Rows.Kind,
            SourceTable = output.Rows.TableName,
            IndexColumn = output.Rows.IndexColumn,
            ExecutionOrder = order,
        });
        for (var position = 0; position < output.Rows.GroupBy.Count; position++)
        {
            built.TableOutputGroupColumns.Add(new BuiltTransformationTableOutputGroupColumnConfiguration
            {
                OutputName = output.Name,
                ColumnName = output.Rows.GroupBy[position],
                Ordinal = position,
            });
        }

        WriteFormula(built, output.Name, null, WhereRole, output.Rows.Condition);
        WriteFormula(built, output.Name, null, CountRole, output.Rows.CountFormula);
        return WriteColumns(built, output, set);
    }

    private static IGenericResult WriteColumns(BuiltTransformationConfiguration built, TableOutputDefinition output, string set)
    {
        for (var position = 0; position < output.Columns.Count; position++)
        {
            var column = output.Columns[position];
            var literal = Literal(set, output.Name, column);
            if (literal.IsFailure)
            {
                return literal;
            }

            built.TableOutputColumns.Add(new BuiltTransformationTableOutputColumnConfiguration
            {
                OutputName = output.Name,
                Name = column.Name,
                Kind = column.Kind,
                SourceName = column.Source,
                LiteralText = literal.Value.Text,
                LiteralType = literal.Value.Type,
                ExecutionOrder = position,
            });
            WriteFormula(built, output.Name, column.Name, ColumnRole, column.Formula);
        }

        return GenericResult.Success();
    }

    private static void WriteFormula(BuiltTransformationConfiguration built, string output, string? column, string role, CalculationNode? formula)
    {
        if (formula is null)
        {
            return;
        }

        foreach (var piece in BuiltFormulaTrees.Flatten(
            formula,
            () => new BuiltTransformationTableOutputNodeConfiguration { OutputName = output, ColumnName = column, Role = role }))
        {
            built.TableOutputNodes.Add(piece);
        }
    }

    // A literal column's stated value is stored as text with its type; every other column kind has none.
    private static IGenericResult<(string? Text, string? Type)> Literal(string set, string output, OutputColumnDefinition column)
    {
        var stated = column.Literal;
        if (!string.Equals(column.Kind, "Literal", StringComparison.Ordinal) || stated is null)
        {
            return GenericResult<(string? Text, string? Type)>.Success((null, null));
        }

        return stated switch
        {
            string text => GenericResult<(string? Text, string? Type)>.Success((text, "Text")),
            bool flag => GenericResult<(string? Text, string? Type)>.Success((flag ? "true" : "false", "Flag")),
            decimal number => GenericResult<(string? Text, string? Type)>.Success((number.ToString(CultureInfo.InvariantCulture), "Number")),
            int whole => GenericResult<(string? Text, string? Type)>.Success((whole.ToString(CultureInfo.InvariantCulture), "Number")),
            _ => SetFailure.Of<(string? Text, string? Type)>(
                "SetDefinitionInvalid", set, $"column '{column.Name}' of output '{output}' states a {stated.GetType().Name}; only text, a number or a flag can be stored"),
        };
    }
}
