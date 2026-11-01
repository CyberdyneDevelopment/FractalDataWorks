using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// INFORMATION_SCHEMA.COLUMNS: the columns of every table the caller may read.
/// </summary>
[TypeOption(typeof(CatalogViews), "InformationSchemaColumns", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InformationSchemaColumnsView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("TABLE_CATALOG", nullable: true),
        SysnameColumn("TABLE_SCHEMA", nullable: true),
        SysnameColumn("TABLE_NAME"),
        SysnameColumn("COLUMN_NAME", nullable: true),
        IntColumn("ORDINAL_POSITION", nullable: true),
        TextColumn("COLUMN_DEFAULT", 4000, nullable: true),
        TextColumn("IS_NULLABLE", 3, nullable: true),
        SysnameColumn("DATA_TYPE", nullable: true),
        IntColumn("CHARACTER_MAXIMUM_LENGTH", nullable: true),
        IntColumn("CHARACTER_OCTET_LENGTH", nullable: true),
        TinyIntColumn("NUMERIC_PRECISION", nullable: true),
        SmallIntColumn("NUMERIC_PRECISION_RADIX", nullable: true),
        IntColumn("NUMERIC_SCALE", nullable: true),
        SmallIntColumn("DATETIME_PRECISION", nullable: true),
        SysnameColumn("CHARACTER_SET_CATALOG", nullable: true),
        SysnameColumn("CHARACTER_SET_SCHEMA", nullable: true),
        SysnameColumn("CHARACTER_SET_NAME", nullable: true),
        SysnameColumn("COLLATION_CATALOG", nullable: true),
        SysnameColumn("COLLATION_SCHEMA", nullable: true),
        SysnameColumn("COLLATION_NAME", nullable: true),
        SysnameColumn("DOMAIN_CATALOG", nullable: true),
        SysnameColumn("DOMAIN_SCHEMA", nullable: true),
        SysnameColumn("DOMAIN_NAME", nullable: true),
    ];

    /// <summary>Initializes a new instance of the <see cref="InformationSchemaColumnsView"/> class.</summary>
    public InformationSchemaColumnsView()
        : base(name: "InformationSchemaColumns", schema: "INFORMATION_SCHEMA", viewName: "COLUMNS")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
        => catalog.Tables.SelectMany(t => t.Table.Columns.Select(c => Column(t.Table, c, context)));

    private static object?[] Column(SqlTable table, SqlTableColumn column, SqlExecutionContext context)
    {
        var facts = ColumnFacts.Of(column, context);
        return
        [
            context.Server.DatabaseName, table.Schema, table.Name, column.Name, column.Ordinal, null,
            column.IsNullable ? "YES" : "NO", facts.TypeName, facts.CharacterLength, facts.OctetLength,
            facts.NumericPrecision, facts.NumericRadix, facts.NumericScale, facts.DateTimePrecision,
            null, null, facts.CollationName is null ? null : "UNICODE", null, null, facts.CollationName, null, null, null,
        ];
    }
}
