using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// sys.all_columns: the columns of the user tables, as sys.columns lists them.
/// </summary>
[TypeOption(typeof(CatalogViews), "SysAllColumns", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysAllColumnsView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        IntColumn("object_id"),
        SysnameColumn("name", nullable: true),
        IntColumn("column_id"),
        TinyIntColumn("system_type_id"),
        IntColumn("user_type_id"),
        SmallIntColumn("max_length"),
        TinyIntColumn("precision"),
        TinyIntColumn("scale"),
        SysnameColumn("collation_name", nullable: true),
        BitColumn("is_nullable", nullable: true),
        BitColumn("is_ansi_padded"),
        BitColumn("is_rowguidcol"),
        BitColumn("is_identity"),
        BitColumn("is_computed"),
        BitColumn("is_filestream"),
        BitColumn("is_replicated", nullable: true),
        BitColumn("is_non_sql_subscribed", nullable: true),
        BitColumn("is_merge_published", nullable: true),
        BitColumn("is_dts_replicated", nullable: true),
        BitColumn("is_xml_document"),
        IntColumn("xml_collection_id"),
        IntColumn("default_object_id"),
        IntColumn("rule_object_id"),
        BitColumn("is_sparse", nullable: true),
        BitColumn("is_column_set", nullable: true),
        TinyIntColumn("generated_always_type", nullable: true),
        TextColumn("generated_always_type_desc", 60, nullable: true),
        IntColumn("encryption_type", nullable: true),
        BitColumn("is_hidden", nullable: true),
        BitColumn("is_masked", nullable: true),
        IntColumn("graph_type", nullable: true),
    ];

    /// <summary>Initializes a new instance of the <see cref="SysAllColumnsView"/> class.</summary>
    public SysAllColumnsView()
        : base(name: "SysAllColumns", schema: "sys", viewName: "all_columns")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
        => catalog.Tables.SelectMany(t => t.Table.Columns.Select(c => Column(t, c, context)));

    private static object?[] Column(CatalogTable table, SqlTableColumn column, SqlExecutionContext context)
    {
        var facts = ColumnFacts.Of(column, context);
        return
        [
            table.ObjectId, column.Name, column.Ordinal, facts.SystemTypeId, (int)facts.SystemTypeId, facts.MaxLength,
            facts.Precision, facts.Scale, facts.CollationName, column.IsNullable, true, false, false, false, false,
            false, false, false, false, false, 0, 0, 0, false, false, (byte)0, "NOT_APPLICABLE", null, false, false, null,
        ];
    }
}
