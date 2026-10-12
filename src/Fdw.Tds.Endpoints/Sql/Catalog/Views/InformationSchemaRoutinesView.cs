using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// INFORMATION_SCHEMA.ROUTINES: procedures and functions; this server has none, so the view is empty.
/// </summary>
[TypeOption(typeof(CatalogViews), "InformationSchemaRoutines", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InformationSchemaRoutinesView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("SPECIFIC_CATALOG", nullable: true),
        SysnameColumn("SPECIFIC_SCHEMA", nullable: true),
        SysnameColumn("SPECIFIC_NAME"),
        SysnameColumn("ROUTINE_CATALOG", nullable: true),
        SysnameColumn("ROUTINE_SCHEMA", nullable: true),
        SysnameColumn("ROUTINE_NAME"),
        TextColumn("ROUTINE_TYPE", 20, nullable: true),
    ];

    /// <summary>Initializes a new instance of the <see cref="InformationSchemaRoutinesView"/> class.</summary>
    public InformationSchemaRoutinesView()
        : base(name: "InformationSchemaRoutines", schema: "INFORMATION_SCHEMA", viewName: "ROUTINES")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => [];
}
