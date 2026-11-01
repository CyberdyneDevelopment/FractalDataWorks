using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// INFORMATION_SCHEMA.TABLES: every table the caller may read, each a BASE TABLE.
/// </summary>
[TypeOption(typeof(CatalogViews), "InformationSchemaTables", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InformationSchemaTablesView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("TABLE_CATALOG", nullable: true),
        SysnameColumn("TABLE_SCHEMA", nullable: true),
        SysnameColumn("TABLE_NAME"),
        TextColumn("TABLE_TYPE", 10, nullable: true),
    ];

    /// <summary>Initializes a new instance of the <see cref="InformationSchemaTablesView"/> class.</summary>
    public InformationSchemaTablesView()
        : base(name: "InformationSchemaTables", schema: "INFORMATION_SCHEMA", viewName: "TABLES")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => catalog.Tables.Select(t => new object?[] { context.Server.DatabaseName, t.Table.Schema, t.Table.Name, "BASE TABLE" });
}
