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
    /// MudBlazor icon for tables.
    /// </summary>
    public const string TableIcon = "table_chart"; // Icons.Material.Filled.TableChart

    /// <summary>
    /// Initializes a new instance of the <see cref="TableObjectType"/> class.
    /// </summary>
    public TableObjectType()
        : base(
            id: 3,
            name: "Table",
            icon: TableIcon,
            color: "Success",
            cssClass: "schema-table",
            canHaveChildren: true,
            sortOrder: 3)
    {
    }
}
