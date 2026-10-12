using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// INFORMATION_SCHEMA.KEY_COLUMN_USAGE: the key fields of every table whose data set declares them, in key order.
/// </summary>
[TypeOption(typeof(CatalogViews), "InformationSchemaKeyColumnUsage", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InformationSchemaKeyColumnUsageView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("CONSTRAINT_CATALOG", nullable: true),
        SysnameColumn("CONSTRAINT_SCHEMA", nullable: true),
        SysnameColumn("CONSTRAINT_NAME"),
        SysnameColumn("TABLE_CATALOG", nullable: true),
        SysnameColumn("TABLE_SCHEMA", nullable: true),
        SysnameColumn("TABLE_NAME"),
        SysnameColumn("COLUMN_NAME", nullable: true),
        IntColumn("ORDINAL_POSITION"),
    ];

    /// <summary>Initializes a new instance of the <see cref="InformationSchemaKeyColumnUsageView"/> class.</summary>
    public InformationSchemaKeyColumnUsageView()
        : base(name: "InformationSchemaKeyColumnUsage", schema: "INFORMATION_SCHEMA", viewName: "KEY_COLUMN_USAGE")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => catalog.Tables.SelectMany(t => t.Table.Columns
        .Where(c => c.IsKey)
        .Select((c, i) => new object?[]
        {
            context.Server.DatabaseName, t.Table.Schema, KeyNames.Of(t.Table), context.Server.DatabaseName,
            t.Table.Schema, t.Table.Name, c.Name, i + 1,
        }));
}
