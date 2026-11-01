using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// sys.types: the SQL Server types this server writes, and sysname.
/// </summary>
[TypeOption(typeof(CatalogViews), "SysTypes", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysTypesView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("name"),
        TinyIntColumn("system_type_id"),
        IntColumn("user_type_id"),
        IntColumn("schema_id"),
        IntColumn("principal_id", nullable: true),
        SmallIntColumn("max_length"),
        TinyIntColumn("precision"),
        TinyIntColumn("scale"),
        SysnameColumn("collation_name", nullable: true),
        BitColumn("is_nullable", nullable: true),
        BitColumn("is_user_defined"),
        BitColumn("is_assembly_type"),
        IntColumn("default_object_id"),
        IntColumn("rule_object_id"),
        BitColumn("is_table_type"),
    ];

    /// <summary>Initializes a new instance of the <see cref="SysTypesView"/> class.</summary>
    public SysTypesView()
        : base(name: "SysTypes", schema: "sys", viewName: "types")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    {
        ArgumentNullException.ThrowIfNull(context);
        var collation = context.Server.Collation;
        foreach (var type in Fdw.Protocols.Tds.Types.SqlDataTypes.All())
        {
            var facts = ColumnFacts.Of(Widest(type), collation);
            yield return
            [
                type.SqlName, type.SystemTypeId, (int)type.SystemTypeId, CatalogIds.Sys, null, facts.MaxLength,
                facts.Precision, facts.Scale, facts.CollationName, true, false, false, 0, 0, false,
            ];
        }

        yield return
        [
            "sysname", (byte)231, Functions.CatalogLookup.SysnameTypeId, CatalogIds.Sys, null, (short)256,
            (byte)0, (byte)0, collation.Name, false, false, false, 0, 0, false,
        ];
    }

    // A type as sys.types describes it: at its largest length, precision and scale.
    private static Fdw.Protocols.Tds.Types.TdsColumnType Widest(Fdw.Protocols.Tds.Types.ISqlDataType type)
        => new(type, type.HasLength ? (type.Holds(typeof(string)) ? 4000 : 8000) : 0, type.HasPrecision ? (byte)38 : (byte)0, type.HasPrecision ? (byte)38 : type.HasScale ? (byte)7 : (byte)0);
}
