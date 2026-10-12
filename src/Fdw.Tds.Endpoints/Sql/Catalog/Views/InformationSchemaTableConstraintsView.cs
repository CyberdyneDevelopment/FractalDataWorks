using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// INFORMATION_SCHEMA.TABLE_CONSTRAINTS: a PRIMARY KEY for every table whose data set declares key fields.
/// </summary>
[TypeOption(typeof(CatalogViews), "InformationSchemaTableConstraints", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InformationSchemaTableConstraintsView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("CONSTRAINT_CATALOG", nullable: true),
        SysnameColumn("CONSTRAINT_SCHEMA", nullable: true),
        SysnameColumn("CONSTRAINT_NAME"),
        SysnameColumn("TABLE_CATALOG", nullable: true),
        SysnameColumn("TABLE_SCHEMA", nullable: true),
        SysnameColumn("TABLE_NAME"),
        TextColumn("CONSTRAINT_TYPE", 11, nullable: true),
        TextColumn("IS_DEFERRABLE", 2),
        TextColumn("INITIALLY_DEFERRED", 2),
    ];

    /// <summary>Initializes a new instance of the <see cref="InformationSchemaTableConstraintsView"/> class.</summary>
    public InformationSchemaTableConstraintsView()
        : base(name: "InformationSchemaTableConstraints", schema: "INFORMATION_SCHEMA", viewName: "TABLE_CONSTRAINTS")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => catalog.Tables
        .Where(t => t.Table.Columns.Any(c => c.IsKey))
        .Select(t => new object?[]
        {
            context.Server.DatabaseName, t.Table.Schema, KeyNames.Of(t.Table), context.Server.DatabaseName,
            t.Table.Schema, t.Table.Name, "PRIMARY KEY", "NO", "NO",
        });
}
