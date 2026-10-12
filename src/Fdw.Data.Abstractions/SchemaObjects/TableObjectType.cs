using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Schema object type for tables.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaObjectTypes), "Table", RestrictToCurrentCompilation = true)]
public sealed class TableObjectType : SchemaObjectTypeBase
{
    /// <summary>
    /// Material Symbols icon name for tables.
    /// </summary>
    public const string TableIcon = "table_chart";

    /// <summary>
    /// Initializes a new instance of the <see cref="TableObjectType"/> class.
    /// </summary>
    public TableObjectType()
        : base(
            name: "Table",
            icon: TableIcon,
            color: "Success",
            cssClass: "schema-table",
            canHaveChildren: true,
            sortOrder: 3)
    {
    }
}
