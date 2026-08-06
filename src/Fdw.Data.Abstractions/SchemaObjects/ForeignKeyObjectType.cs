using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Schema object type for foreign keys.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaObjectTypes), "ForeignKey", RestrictToCurrentCompilation = true)]
public sealed class ForeignKeyObjectType : SchemaObjectTypeBase
{
    /// <summary>
    /// MudBlazor icon for foreign keys.
    /// </summary>
    public const string ForeignKeyIcon = "link"; // Icons.Material.Filled.Link

    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKeyObjectType"/> class.
    /// </summary>
    public ForeignKeyObjectType()
        : base(
            id: 8,
            name: "ForeignKey",
            icon: ForeignKeyIcon,
            color: "Tertiary",
            cssClass: "schema-foreignkey",
            canHaveChildren: false,
            sortOrder: 8)
    {
    }
}
