using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// INFORMATION_SCHEMA.VIEWS: views; this server has none, so the view is empty.
/// </summary>
[TypeOption(typeof(CatalogViews), "InformationSchemaViews", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InformationSchemaViewsView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("TABLE_CATALOG", nullable: true),
        SysnameColumn("TABLE_SCHEMA", nullable: true),
        SysnameColumn("TABLE_NAME"),
    ];

    /// <summary>Initializes a new instance of the <see cref="InformationSchemaViewsView"/> class.</summary>
    public InformationSchemaViewsView()
        : base(name: "InformationSchemaViews", schema: "INFORMATION_SCHEMA", viewName: "VIEWS")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => [];
}
