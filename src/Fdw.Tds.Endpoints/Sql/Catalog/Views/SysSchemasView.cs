using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// sys.schemas: SQL Server's own schemas, and one schema per workspace the caller may read.
/// </summary>
[TypeOption(typeof(CatalogViews), "SysSchemas", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysSchemasView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("name"),
        IntColumn("schema_id"),
        IntColumn("principal_id", nullable: true),
    ];

    /// <summary>Initializes a new instance of the <see cref="SysSchemasView"/> class.</summary>
    public SysSchemasView()
        : base(name: "SysSchemas", schema: "sys", viewName: "schemas")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => catalog.Schemas.Select(s => new object?[] { s.Name, s.SchemaId, 1 });
}
