using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// sys.tables: one user table per data set the caller may read.
/// </summary>
[TypeOption(typeof(CatalogViews), "SysTables", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysTablesView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("name"),
        IntColumn("object_id"),
        IntColumn("principal_id", nullable: true),
        IntColumn("schema_id"),
        IntColumn("parent_object_id"),
        TextColumn("type", 2),
        TextColumn("type_desc", 60, nullable: true),
        DateTimeColumn("create_date"),
        DateTimeColumn("modify_date"),
        BitColumn("is_ms_shipped"),
        BitColumn("is_published"),
        BitColumn("is_schema_published"),
        IntColumn("lob_data_space_id"),
        IntColumn("filestream_data_space_id", nullable: true),
        IntColumn("max_column_id_used"),
        BitColumn("lock_on_bulk_load"),
        BitColumn("uses_ansi_nulls", nullable: true),
        BitColumn("is_replicated", nullable: true),
        BitColumn("has_replication_filter", nullable: true),
        BitColumn("is_merge_published", nullable: true),
        BitColumn("is_sync_tran_subscribed", nullable: true),
        BitColumn("has_unchecked_assembly_data"),
        IntColumn("text_in_row_limit", nullable: true),
        BitColumn("large_value_types_out_of_row", nullable: true),
        BitColumn("is_tracked_by_cdc", nullable: true),
        TinyIntColumn("lock_escalation", nullable: true),
        TextColumn("lock_escalation_desc", 60, nullable: true),
        BitColumn("is_filetable", nullable: true),
        BitColumn("is_memory_optimized", nullable: true),
        TinyIntColumn("durability", nullable: true),
        TextColumn("durability_desc", 60, nullable: true),
        TinyIntColumn("temporal_type", nullable: true),
        TextColumn("temporal_type_desc", 60, nullable: true),
        IntColumn("history_table_id", nullable: true),
        BitColumn("is_remote_data_archive_enabled", nullable: true),
        BitColumn("is_external"),
        BitColumn("is_node", nullable: true),
        BitColumn("is_edge", nullable: true),
        TinyIntColumn("ledger_type", nullable: true),
        BitColumn("is_dropped_ledger_table", nullable: true),
    ];

    /// <summary>Initializes a new instance of the <see cref="SysTablesView"/> class.</summary>
    public SysTablesView()
        : base(name: "SysTables", schema: "sys", viewName: "tables")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => catalog.Tables.Select(t => new object?[]
    {
        t.Table.Name, t.ObjectId, null, t.SchemaId, 0, "U ", "USER_TABLE", t.Table.Created, t.Table.Modified, false, false, false,
        0, null, t.Table.Columns.Count, false, true, false, false, false, false, false, 0, false, false,
        (byte)0, "TABLE", false, false, (byte)0, "SCHEMA_AND_DATA", (byte)0, "NON_TEMPORAL_TABLE", null, false,
        false, false, false, (byte)0, false,
    });
}
