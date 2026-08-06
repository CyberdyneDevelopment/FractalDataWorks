using System;
using System.Collections.Generic;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a detail/edit page for viewing and modifying a single entity.
/// </summary>
/// <remarks>
/// Detail pages extend the basic page model with:
/// - Entity metadata (ID, type, timestamps)
/// - Save/Cancel/Delete actions
/// - Dirty state tracking
/// - Related entity navigation
/// </remarks>
public interface IDetailPageModel : IPageModel
{
    /// <summary>
    /// Gets the entity identifier being edited.
    /// </summary>
    object? EntityId { get; }

    /// <summary>
    /// Gets the entity type name (e.g., "MsSqlConfiguration").
    /// </summary>
    string EntityTypeName { get; }

    /// <summary>
    /// Gets the display name for the entity type (e.g., "SQL Server Connection").
    /// </summary>
    string EntityTypeDisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether this is a new entity.
    /// </summary>
    bool IsNew { get; }

    /// <summary>
    /// Gets the available actions (Save, Cancel, Delete, etc.).
    /// </summary>
    IReadOnlyList<IPageAction> Actions { get; }

    /// <summary>
    /// Gets related entities that can be navigated to.
    /// </summary>
    IReadOnlyList<IRelatedEntity> RelatedEntities { get; }

    /// <summary>
    /// Gets the entity creation timestamp (if available).
    /// </summary>
    DateTime? CreatedAt { get; }

    /// <summary>
    /// Gets the entity last modified timestamp (if available).
    /// </summary>
    DateTime? ModifiedAt { get; }

    /// <summary>
    /// Gets the user who created the entity (if available).
    /// </summary>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the user who last modified the entity (if available).
    /// </summary>
    string? ModifiedBy { get; }

    /// <summary>
    /// Gets a value indicating whether delete is allowed.
    /// </summary>
    bool CanDelete { get; }

    /// <summary>
    /// Gets the breadcrumb trail for navigation context.
    /// </summary>
    IReadOnlyList<IBreadcrumbItem> Breadcrumbs { get; }
}