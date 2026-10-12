using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Components.Models;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a detail/edit page model.
/// </summary>
public sealed class DetailPageModel : IDetailPageModel
{
    private readonly List<SectionModel> _sections = [];
    private readonly List<PageAction> _actions = [];
    private readonly List<RelatedEntity> _relatedEntities = [];
    private readonly List<BreadcrumbItem> _breadcrumbs = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Title { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ISectionModel> Sections => _sections;

    /// <inheritdoc />
    public IPageMode Mode { get; set; } = PageModes.View;

    /// <inheritdoc />
    public bool HasChanges { get; set; }

    /// <inheritdoc />
    public object? EntityId { get; set; }

    /// <inheritdoc />
    public string EntityTypeName { get; set; } = "";

    /// <inheritdoc />
    public string EntityTypeDisplayName { get; set; } = "";

    /// <inheritdoc />
    public bool IsNew => EntityId == null || Mode.IsCreateMode;

    /// <inheritdoc />
    public IReadOnlyList<IPageAction> Actions => _actions;

    /// <inheritdoc />
    public IReadOnlyList<IRelatedEntity> RelatedEntities => _relatedEntities;

    /// <inheritdoc />
    public DateTime? CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTime? ModifiedAt { get; set; }

    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; set; }

    /// <inheritdoc />
    public bool CanDelete { get; set; } = true;

    /// <inheritdoc />
    public IReadOnlyList<IBreadcrumbItem> Breadcrumbs => _breadcrumbs;

    /// <summary>
    /// Adds a section to the page.
    /// </summary>
    public void AddSection(SectionModel section) => _sections.Add(section);

    /// <summary>
    /// Adds an action to the page.
    /// </summary>
    public void AddAction(PageAction action) => _actions.Add(action);

    /// <summary>
    /// Adds a related entity reference.
    /// </summary>
    public void AddRelatedEntity(RelatedEntity entity) => _relatedEntities.Add(entity);

    /// <summary>
    /// Adds a breadcrumb item.
    /// </summary>
    public void AddBreadcrumb(BreadcrumbItem item) => _breadcrumbs.Add(item);

    /// <summary>
    /// Validates all components in the page.
    /// </summary>
    public ValidationResult Validate()
    {
        var results = _sections
            .SelectMany(s => s.AllComponents)
            .Select(c => c.Validate())
            .ToList();

        return ValidationResult.Combine(results.ToArray());
    }

    /// <summary>
    /// Adds standard CRUD actions (Save, Cancel, Delete).
    /// </summary>
    public void AddStandardActions()
    {
        _actions.Add(new PageAction
        {
            Id = "save",
            Label = "Save",
            Icon = "💾",
            Shortcut = 's'
        });

        _actions.Add(new PageAction
        {
            Id = "cancel",
            Label = "Cancel",
            Icon = "✕",
            Shortcut = 'c'
        });

        if (!IsNew && CanDelete)
        {
            _actions.Add(new PageAction
            {
                Id = "delete",
                Label = "Delete",
                Icon = "🗑",
                IsDestructive = true,
                RequiresConfirmation = true,
                Shortcut = 'd'
            });
        }
    }

    /// <summary>
    /// Creates a new detail page for creating an entity.
    /// </summary>
    public static DetailPageModel ForCreate(string entityTypeName, string displayName)
    {
        var page = new DetailPageModel
        {
            Id = $"create-{entityTypeName.ToLowerInvariant()}",
            Title = $"New {displayName}",
            EntityTypeName = entityTypeName,
            EntityTypeDisplayName = displayName,
            Mode = PageModes.Create
        };
        page.AddStandardActions();
        return page;
    }

    /// <summary>
    /// Creates a new detail page for editing an entity.
    /// </summary>
    public static DetailPageModel ForEdit(string entityTypeName, string displayName, object entityId, string? entityName = null)
    {
        var page = new DetailPageModel
        {
            Id = $"edit-{entityTypeName.ToLowerInvariant()}-{entityId}",
            Title = entityName ?? $"Edit {displayName}",
            EntityTypeName = entityTypeName,
            EntityTypeDisplayName = displayName,
            EntityId = entityId,
            Mode = PageModes.Edit
        };
        page.AddStandardActions();
        return page;
    }

    /// <summary>
    /// Creates a new detail page for viewing an entity (read-only).
    /// </summary>
    public static DetailPageModel ForView(string entityTypeName, string displayName, object entityId, string? entityName = null)
    {
        var page = new DetailPageModel
        {
            Id = $"view-{entityTypeName.ToLowerInvariant()}-{entityId}",
            Title = entityName ?? displayName,
            EntityTypeName = entityTypeName,
            EntityTypeDisplayName = displayName,
            EntityId = entityId,
            Mode = PageModes.View,
            CanDelete = false
        };
        page.AddAction(new PageAction { Id = "edit", Label = "Edit", Icon = "✏", Shortcut = 'e' });
        page.AddAction(new PageAction { Id = "back", Label = "Back", Icon = "←", Shortcut = 'b' });
        return page;
    }
}