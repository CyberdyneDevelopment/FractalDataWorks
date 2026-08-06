using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Schema object type for views.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaObjectTypes), "View", RestrictToCurrentCompilation = true)]
public sealed class ViewObjectType : SchemaObjectTypeBase
{
    /// <summary>
    /// MudBlazor icon for views.
    /// </summary>
    public const string ViewIcon = "view_module"; // Icons.Material.Filled.ViewModule

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewObjectType"/> class.
    /// </summary>
    public ViewObjectType()
        : base(
            id: 4,
            name: "View",
            icon: ViewIcon,
            color: "Info",
            cssClass: "schema-view",
            canHaveChildren: true,
            sortOrder: 4)
    {
    }
}
