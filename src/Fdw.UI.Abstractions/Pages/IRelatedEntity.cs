namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a related entity for navigation.
/// </summary>
public interface IRelatedEntity
{
    /// <summary>
    /// Gets the relationship type (e.g., "Uses", "References", "Parent").
    /// </summary>
    string RelationshipType { get; }

    /// <summary>
    /// Gets the related entity type name.
    /// </summary>
    string EntityTypeName { get; }

    /// <summary>
    /// Gets the related entity display name.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the navigation target (e.g., "connections/mssql/123").
    /// </summary>
    string NavigationTarget { get; }

    /// <summary>
    /// Gets the icon for the relationship.
    /// </summary>
    string? Icon { get; }
}