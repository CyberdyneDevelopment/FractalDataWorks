using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Schema object type for indexes.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaObjectTypes), "Index", RestrictToCurrentCompilation = true)]
public sealed class IndexObjectType : SchemaObjectTypeBase
{
    /// <summary>
    /// MudBlazor icon for indexes.
    /// </summary>
    public const string IndexIcon = "flash_on"; // Icons.Material.Filled.FlashOn

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexObjectType"/> class.
    /// </summary>
    public IndexObjectType()
        : base(
            id: 7,
            name: "Index",
            icon: IndexIcon,
            color: "Secondary",
            cssClass: "schema-index",
            canHaveChildren: false,
            sortOrder: 7)
    {
    }
}
