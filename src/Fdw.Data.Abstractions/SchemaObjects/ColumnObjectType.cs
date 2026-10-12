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
    /// Material Symbols icon name for columns.
    /// </summary>
    public const string ColumnIcon = "view_column";

    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnObjectType"/> class.
    /// </summary>
    public ColumnObjectType()
        : base(
            name: "Column",
            icon: ColumnIcon,
            color: "Default",
            cssClass: "schema-column",
            canHaveChildren: false,
            sortOrder: 6)
    {
    }
}
