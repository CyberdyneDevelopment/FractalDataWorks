using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// sys.foreign_keys: foreign keys; none are declared, so the view is empty.
/// </summary>
[TypeOption(typeof(CatalogViews), "SysForeignKeys", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysForeignKeysView : CatalogViewBase
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
        IntColumn("referenced_object_id", nullable: true),
        IntColumn("key_index_id", nullable: true),
        BitColumn("is_disabled"),
        BitColumn("is_not_for_replication"),
        BitColumn("is_not_trusted"),
        TinyIntColumn("delete_referential_action", nullable: true),
        TextColumn("delete_referential_action_desc", 60, nullable: true),
        TinyIntColumn("update_referential_action", nullable: true),
        TextColumn("update_referential_action_desc", 60, nullable: true),
        BitColumn("is_system_named"),
    ];

    /// <summary>Initializes a new instance of the <see cref="SysForeignKeysView"/> class.</summary>
    public SysForeignKeysView()
        : base(name: "SysForeignKeys", schema: "sys", viewName: "foreign_keys")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    => [];
}
