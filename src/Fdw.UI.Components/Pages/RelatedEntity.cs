using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a related entity reference.
/// </summary>
public sealed class RelatedEntity : IRelatedEntity
{
    /// <inheritdoc />
    public string RelationshipType { get; set; } = "";

    /// <inheritdoc />
    public string EntityTypeName { get; set; } = "";

    /// <inheritdoc />
    public string DisplayName { get; set; } = "";

    /// <inheritdoc />
    public string NavigationTarget { get; set; } = "";

    /// <inheritdoc />
    public string? Icon { get; set; }

    /// <summary>
    /// Creates a "uses" relationship (e.g., Pipeline uses Connection).
    /// </summary>
    public static RelatedEntity Uses(string entityType, string displayName, string target) =>
        new() { RelationshipType = "Uses", EntityTypeName = entityType, DisplayName = displayName, NavigationTarget = target, Icon = "→" };

    /// <summary>
    /// Creates a "used by" relationship (e.g., Connection used by Pipeline).
    /// </summary>
    public static RelatedEntity UsedBy(string entityType, string displayName, string target) =>
        new() { RelationshipType = "Used by", EntityTypeName = entityType, DisplayName = displayName, NavigationTarget = target, Icon = "←" };

    /// <summary>
    /// Creates a "parent" relationship.
    /// </summary>
    public static RelatedEntity Parent(string entityType, string displayName, string target) =>
        new() { RelationshipType = "Parent", EntityTypeName = entityType, DisplayName = displayName, NavigationTarget = target, Icon = "↑" };
}