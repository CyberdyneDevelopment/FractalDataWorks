using Fdw.Collections;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Base class for schema object type definitions with UI properties.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the foundation for schema object types used in UI rendering.
/// Each derived class represents a specific type of database object with associated
/// visual properties for consistent display.
/// </para>
/// </remarks>
public abstract class SchemaObjectTypeBase : TypeOptionBase<int, SchemaObjectTypeBase>, ISchemaObjectType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaObjectTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this object type.</param>
    /// <param name="name">The name of this object type.</param>
    /// <param name="icon">The MudBlazor icon name.</param>
    /// <param name="color">The MudBlazor color.</param>
    /// <param name="cssClass">Additional CSS class for styling.</param>
    /// <param name="canHaveChildren">Whether this type can have child objects.</param>
    /// <param name="sortOrder">Sort order for tree display.</param>
    protected SchemaObjectTypeBase(
        int id,
        string name,
        string icon,
        string color,
        string cssClass,
        bool canHaveChildren,
        int sortOrder)
        : base(id, name)
    {
        Icon = icon;
        Color = color;
        CssClass = cssClass;
        CanHaveChildren = canHaveChildren;
        SortOrder = sortOrder;
    }

    /// <inheritdoc/>
    public string Icon { get; }

    /// <inheritdoc/>
    public string Color { get; }

    /// <inheritdoc/>
    public string CssClass { get; }

    /// <inheritdoc/>
    public bool CanHaveChildren { get; }

    /// <inheritdoc/>
    public int SortOrder { get; }
}
