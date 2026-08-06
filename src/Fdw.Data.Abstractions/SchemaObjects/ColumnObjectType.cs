using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Schema object type for columns.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaObjectTypes), "Column", RestrictToCurrentCompilation = true)]
public sealed class ColumnObjectType : SchemaObjectTypeBase
{
    /// <summary>
    /// MudBlazor icon for columns.
    /// </summary>
    public const string ColumnIcon = "view_column"; // Icons.Material.Filled.ViewColumn

    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnObjectType"/> class.
    /// </summary>
    public ColumnObjectType()
        : base(
            id: 6,
            name: "Column",
            icon: ColumnIcon,
            color: "Default",
            cssClass: "schema-column",
            canHaveChildren: false,
            sortOrder: 6)
    {
    }
}
