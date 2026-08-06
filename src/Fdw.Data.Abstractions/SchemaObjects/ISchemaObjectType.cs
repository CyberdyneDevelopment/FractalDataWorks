using Fdw.Collections;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Interface for schema object types with UI rendering properties.
/// </summary>
/// <remarks>
/// <para>
/// Schema object types provide metadata for rendering database objects in UI components.
/// Each type includes visual properties like icons and colors for consistent display
/// across the application.
/// </para>
/// <para>
/// Supported object types:
/// <list type="bullet">
/// <item><description>Database - Top-level database container</description></item>
/// <item><description>Schema - Database schema (e.g., dbo, sales)</description></item>
/// <item><description>Table - Database table</description></item>
/// <item><description>View - Database view</description></item>
/// <item><description>StoredProcedure - Stored procedure</description></item>
/// <item><description>Column - Table/view column</description></item>
/// <item><description>Index - Table index</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ISchemaObjectType : ITypeOption<int, SchemaObjectTypeBase>
{
    /// <summary>
    /// Gets the MudBlazor icon name for this object type.
    /// </summary>
    /// <example>@Icons.Material.Filled.TableChart</example>
    string Icon { get; }

    /// <summary>
    /// Gets the MudBlazor color for this object type.
    /// </summary>
    /// <example>Primary, Success, Info, Warning, Error</example>
    string Color { get; }

    /// <summary>
    /// Gets the additional CSS class for custom styling.
    /// </summary>
    string CssClass { get; }

    /// <summary>
    /// Gets whether this object type can have child objects.
    /// </summary>
    bool CanHaveChildren { get; }

    /// <summary>
    /// Gets the sort order for display in tree views.
    /// </summary>
    int SortOrder { get; }
}
