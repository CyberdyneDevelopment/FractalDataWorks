using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// sys.objects: one user table per data set the caller may read.
/// </summary>
[TypeOption(typeof(CatalogViews), "SysObjects", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysObjectsView : CatalogViewBase
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

    /// <summary>Initializes a new instance of the <see cref="SysObjectsView"/> class.</summary>
    public SysObjectsView()
        : base(name: "SysObjects", schema: "sys", viewName: "objects")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => catalog.Tables.Select(t => new object?[] { t.Table.Name, t.ObjectId, null, t.SchemaId, 0, "U ", "USER_TABLE", t.Table.Created, t.Table.Modified, false, false, false });
}
