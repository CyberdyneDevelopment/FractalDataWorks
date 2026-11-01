using System.Linq;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Describes a calculation set definition as the wire payloads a read returns, the reverse of <see cref="BuiltSetMapper"/>.</summary>
internal static class BuiltSetPayloads
{
    internal static TableInputPayload Of(TableInputDefinition table) => new()
    {
        Name = table.Name,
        Columns = [.. table.Columns.Select(column => new TableColumnPayload { Name = column.Name, ValueType = column.ValueType })],
    };

    internal static DerivedValuePayload Of(DerivedValueDefinition value) => new()
    {
        Name = value.Name,
        Kind = value.Kind,
        Table = value.Table,
        Column = value.Column,
        KeyColumn = value.KeyColumn,
        MatchField = value.MatchField,
    };

    internal static TableOutputPayload Of(TableOutputDefinition output) => new()
    {
        Name = output.Name,
        Rows = new RowSourcePayload
        {
            Kind = output.Rows.Kind,
            Table = output.Rows.TableName,
            Where = output.Rows.Condition is { } condition ? BuiltFormulaReader.ToPayload(condition) : null,
            Count = output.Rows.CountFormula is { } count ? BuiltFormulaReader.ToPayload(count) : null,
            IndexColumn = output.Rows.IndexColumn,
            By = output.Rows.GroupBy.Count > 0 ? [.. output.Rows.GroupBy] : null,
        },
        Columns = [.. output.Columns.Select(column => Of(column))],
    };

    private static OutputColumnPayload Of(OutputColumnDefinition column)
    {
        var payload = new OutputColumnPayload { Name = column.Name, Kind = column.Kind, Source = column.Source };
        if (column.Formula is { } formula)
        {
            payload.Formula = BuiltFormulaReader.ToPayload(formula);
        }

        if (string.Equals(column.Kind, "Literal", System.StringComparison.Ordinal))
        {
            (payload.LiteralType, payload.LiteralValue) = column.Literal switch
            {
                bool flag => ("Flag", flag ? "true" : "false"),
                decimal number => ("Number", number.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                string text => ("Text", text),
                _ => (null, null),
            };
        }

        return payload;
    }
}
