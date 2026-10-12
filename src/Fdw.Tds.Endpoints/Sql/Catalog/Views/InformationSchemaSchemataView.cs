using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// INFORMATION_SCHEMA.SCHEMATA: every schema, SQL Server's own and one per workspace the caller may read.
/// </summary>
[TypeOption(typeof(CatalogViews), "InformationSchemaSchemata", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InformationSchemaSchemataView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("CATALOG_NAME", nullable: true),
        SysnameColumn("SCHEMA_NAME"),
        SysnameColumn("SCHEMA_OWNER", nullable: true),
        SysnameColumn("DEFAULT_CHARACTER_SET_CATALOG", nullable: true),
        SysnameColumn("DEFAULT_CHARACTER_SET_SCHEMA", nullable: true),
        SysnameColumn("DEFAULT_CHARACTER_SET_NAME", nullable: true),
    ];

    /// <summary>Initializes a new instance of the <see cref="InformationSchemaSchemataView"/> class.</summary>
    public InformationSchemaSchemataView()
        : base(name: "InformationSchemaSchemata", schema: "INFORMATION_SCHEMA", viewName: "SCHEMATA")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => catalog.Schemas.Select(s => new object?[] { context.Server.DatabaseName, s.Name, "dbo", null, null, "iso_1" });
}
