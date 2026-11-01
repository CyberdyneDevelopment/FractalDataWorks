using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// sys.views: views; this server has none, so the view is empty.
/// </summary>
[TypeOption(typeof(CatalogViews), "SysViews", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysViewsView : CatalogViewBase
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
    ];

    /// <summary>Initializes a new instance of the <see cref="SysViewsView"/> class.</summary>
    public SysViewsView()
        : base(name: "SysViews", schema: "sys", viewName: "views")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => [];
}
