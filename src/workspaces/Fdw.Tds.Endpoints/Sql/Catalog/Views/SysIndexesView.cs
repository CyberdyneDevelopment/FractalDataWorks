using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// sys.indexes: indexes; the tables are views over data sets and carry none, so the view is empty.
/// </summary>
[TypeOption(typeof(CatalogViews), "SysIndexes", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysIndexesView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        IntColumn("object_id"),
        SysnameColumn("name", nullable: true),
        IntColumn("index_id"),
        TinyIntColumn("type"),
        TextColumn("type_desc", 60, nullable: true),
        BitColumn("is_unique", nullable: true),
        IntColumn("data_space_id", nullable: true),
        BitColumn("ignore_dup_key", nullable: true),
        BitColumn("is_primary_key", nullable: true),
        BitColumn("is_unique_constraint", nullable: true),
        TinyIntColumn("fill_factor"),
        BitColumn("is_padded", nullable: true),
        BitColumn("is_disabled", nullable: true),
        BitColumn("is_hypothetical", nullable: true),
        BitColumn("allow_row_locks", nullable: true),
        BitColumn("allow_page_locks", nullable: true),
        BitColumn("has_filter", nullable: true),
        TextColumn("filter_definition", -1, nullable: true),
    ];

    /// <summary>Initializes a new instance of the <see cref="SysIndexesView"/> class.</summary>
    public SysIndexesView()
        : base(name: "SysIndexes", schema: "sys", viewName: "indexes")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => [];
}
