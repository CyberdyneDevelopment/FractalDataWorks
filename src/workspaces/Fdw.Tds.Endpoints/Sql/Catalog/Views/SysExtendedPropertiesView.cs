using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// sys.extended_properties: descriptions attached to objects; none are, so the view is empty.
/// </summary>
[TypeOption(typeof(CatalogViews), "SysExtendedProperties", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysExtendedPropertiesView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        TinyIntColumn("class"),
        TextColumn("class_desc", 60, nullable: true),
        IntColumn("major_id"),
        IntColumn("minor_id"),
        SysnameColumn("name"),
        TextColumn("value", 4000, nullable: true),
    ];

    /// <summary>Initializes a new instance of the <see cref="SysExtendedPropertiesView"/> class.</summary>
    public SysExtendedPropertiesView()
        : base(name: "SysExtendedProperties", schema: "sys", viewName: "extended_properties")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => [];
}
