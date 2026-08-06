using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Renders detail/edit pages using Spectre.Console with form fields, breadcrumbs, and actions.
/// </summary>
public sealed class DetailPageRenderer
{
    /// <summary>
    /// Renders a detail page and returns the selected action.
    /// </summary>
    public static DetailPageResult Render(IDetailPageModel page, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        console.Clear();

        // Render breadcrumbs
        RenderBreadcrumbs(page.Breadcrumbs, console, theme);

        // Render header
        RenderHeader(page, console, theme);

        // Render metadata (timestamps)
        RenderMetadata(page, console, theme);

        // Render sections with fields
        foreach (var section in page.Sections.Where(s => s.IsVisible))
        {
            RenderSection(section, console, theme, context);
        }

        // Render related entities
        if (page.RelatedEntities.Count > 0)
        {
            RenderRelatedEntities(page.RelatedEntities, console, theme);
        }

        // Prompt for action
        return PromptAction(page, console, theme);
    }

    private static void RenderBreadcrumbs(IReadOnlyList<IBreadcrumbItem> breadcrumbs, IAnsiConsole console, IMenuTheme theme)
    {
        if (breadcrumbs.Count == 0)
        {
            return;
        }

        var parts = new List<string>();
        foreach (var crumb in breadcrumbs)
        {
            if (crumb.IsCurrent)
            {
                parts.Add($"[{theme.Colors.Primary}]{crumb.Label}[/]");
            }
            else
            {
                parts.Add($"[{theme.Colors.Muted}]{crumb.Label}[/]");
            }
        }

        console.MarkupLine(string.Join($" [{theme.Colors.Muted}]>[/] ", parts));
        console.WriteLine();
    }

    private static void RenderHeader(IDetailPageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var operationIcon = page.IsNew ? "+" : "✏";
        var operationLabel = page.IsNew ? "Create" : "Edit";
        var title = $"{operationIcon} {operationLabel} {page.EntityTypeDisplayName}";

        var panel = new Panel(new Markup(page.Description ?? page.EntityTypeDisplayName))
        {
            Header = new PanelHeader($"[{theme.Colors.Primary} bold]{title}[/]"),
            Border = theme.Borders.Panel,
            BorderStyle = new Style(theme.Colors.Primary),
            Padding = new Padding(1, 0)
        };

        console.Write(panel);
        console.WriteLine();
    }

    private static void RenderMetadata(IDetailPageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        if (!page.IsNew && (page.CreatedAt.HasValue || page.ModifiedAt.HasValue))
        {
            var metaParts = new List<string>();

            if (page.CreatedAt.HasValue)
            {
                var creator = !string.IsNullOrEmpty(page.CreatedBy) ? $" by {page.CreatedBy}" : "";
                metaParts.Add($"Created: {page.CreatedAt.Value.ToString("g", CultureInfo.CurrentCulture)}{creator}");
            }

            if (page.ModifiedAt.HasValue)
            {
                var modifier = !string.IsNullOrEmpty(page.ModifiedBy) ? $" by {page.ModifiedBy}" : "";
                metaParts.Add($"Modified: {page.ModifiedAt.Value.ToString("g", CultureInfo.CurrentCulture)}{modifier}");
            }

            console.MarkupLine($"[{theme.Colors.Muted}]{string.Join(" | ", metaParts)}[/]");
            console.WriteLine();
        }
    }

    private static void RenderSection(ISectionModel section, IAnsiConsole console, IMenuTheme theme, SpectreRenderContext context)
    {
        // Section header
        if (!string.IsNullOrEmpty(section.Title))
        {
            if (section.IsCollapsible)
            {
                var expandIcon = section.IsExpanded ? theme.Icons.ExpandedIcon : theme.Icons.CollapsedIcon;
                console.MarkupLine($"[{theme.Colors.Secondary} bold]{expandIcon} {section.Title}[/]");
            }
            else
            {
                console.MarkupLine($"[{theme.Colors.Secondary} bold]{section.Title}[/]");
            }

            if (!string.IsNullOrEmpty(section.Description))
            {
                console.MarkupLine($"[{theme.Colors.Muted}]{section.Description}[/]");
            }
        }

        if (section.IsCollapsible && !section.IsExpanded)
        {
            console.WriteLine();
            return;
        }

        // Render fields in a grid
        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(30));  // Label column
        grid.AddColumn();  // Value column

        foreach (var component in section.AllComponents.Where(c => c.IsVisible))
        {
            var label = GetComponentLabel(component, theme);
            var value = GetComponentDisplayValue(component, theme, context);

            grid.AddRow(label, value);
        }

        console.Write(grid);
        console.WriteLine();
    }

    private static string GetComponentLabel(IComponentModel component, IMenuTheme theme)
    {
        var requiredMarker = component.IsRequired ? $"[{theme.Colors.Error}]*[/]" : "";
        var labelColor = component.IsRequired ? theme.Colors.Primary : theme.Colors.Foreground;
        return $"[{labelColor}]{component.Label ?? component.Id}[/]{requiredMarker}:";
    }

    private static string GetComponentDisplayValue(IComponentModel component, IMenuTheme theme, SpectreRenderContext context)
    {
        if (component is IInputComponentModel inputModel)
        {
            var value = inputModel.ValueAsObject;

            if (value == null)
            {
                return $"[{theme.Colors.Muted}](not set)[/]";
            }

            // Handle boolean
            if (value is bool boolVal)
            {
                var icon = boolVal ? theme.Icons.CheckedIndicator : theme.Icons.UncheckedIndicator;
                var color = boolVal ? theme.Colors.Success : theme.Colors.Muted;
                return $"[{color}]{icon}[/]";
            }

            // Handle password/secret fields
            if (component.Label?.Contains("Password", StringComparison.OrdinalIgnoreCase) == true ||
                component.Label?.Contains("Secret", StringComparison.OrdinalIgnoreCase) == true ||
                component.Id.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                component.Id.Contains("Secret", StringComparison.OrdinalIgnoreCase))
            {
                return $"[{theme.Colors.Muted}]********[/]";
            }

            // Handle enums/select values
            if (component is ISelectableComponentModel)
            {
                return $"[{theme.Colors.Foreground}]{Markup.Escape(value.ToString() ?? "")}[/]";
            }

            // Default display
            var displayValue = value.ToString() ?? "";
            if (displayValue.Length > 60)
            {
                displayValue = string.Concat(displayValue.AsSpan(0, 57), "...");
            }

            return $"[{theme.Colors.Foreground}]{Markup.Escape(displayValue)}[/]";
        }

        return $"[{theme.Colors.Muted}]-[/]";
    }

    private static void RenderRelatedEntities(IReadOnlyList<IRelatedEntity> related, IAnsiConsole console, IMenuTheme theme)
    {
        console.MarkupLine($"[{theme.Colors.Secondary} bold]Related[/]");

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderColor(theme.Colors.Muted)
            .HideHeaders();

        table.AddColumn("");
        table.AddColumn("");
        table.AddColumn("");

        foreach (var entity in related)
        {
            var icon = entity.Icon ?? "→";
            table.AddRow(
                $"[{theme.Colors.Muted}]{entity.RelationshipType}[/]",
                $"[{theme.Colors.Muted}]{icon}[/]",
                $"[{theme.Colors.Info}]{entity.DisplayName}[/] [{theme.Colors.Muted}]({entity.EntityTypeName})[/]"
            );
        }

        console.Write(table);
        console.WriteLine();
    }

    // MA0051: Method length acceptable - procedural action menu with dynamic action list and confirmation prompts
#pragma warning disable MA0051 // Method is too long
    private static DetailPageResult PromptAction(IDetailPageModel page, IAnsiConsole console, IMenuTheme theme)
#pragma warning restore MA0051
    {
        var choices = new List<(string Id, string Label, bool IsDestructive)>();

        // Add available actions
        foreach (var pageAction in page.Actions.Where(a => a.IsEnabled))
        {
            var shortcut = pageAction.Shortcut.HasValue ? $"[{pageAction.Shortcut}] " : "";
            var label = $"{shortcut}{pageAction.Label}";

            if (pageAction.IsDestructive)
            {
                label = $"[{theme.Colors.Error}]{label}[/]";
            }

            choices.Add((pageAction.Id, label, pageAction.IsDestructive));
        }

        // Always add Edit and Back options
        if (!choices.Any(c => string.Equals(c.Id, "edit", StringComparison.Ordinal)))
        {
            choices.Insert(0, ("edit", "[e] Edit Fields", false));
        }

        choices.Add(("back", "[q] Back", false));

        var prompt = new SelectionPrompt<(string Id, string Label, bool IsDestructive)>()
            .Title($"[{theme.Colors.Primary}]Select action:[/]")
            .AddChoices(choices)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(theme.Colors.Selected));

        var selected = console.Prompt(prompt);

        if (string.Equals(selected.Id, "back", StringComparison.Ordinal))
        {
            return new DetailPageResult { ShouldExit = true };
        }

        if (string.Equals(selected.Id, "edit", StringComparison.Ordinal))
        {
            return PromptEditFields(page, console, theme);
        }

        // Find the action
        var action = page.Actions.FirstOrDefault(a => string.Equals(a.Id, selected.Id, StringComparison.Ordinal));
        if (action == null)
        {
            return new DetailPageResult { ShouldExit = false };
        }

        // Handle confirmation for destructive actions
        if (action.RequiresConfirmation)
        {
            var confirmed = console.Confirm(
                $"[{theme.Colors.Warning}]Are you sure you want to {action.Label.ToLowerInvariant()}?[/]",
                false);

            if (!confirmed)
            {
                return new DetailPageResult { ShouldExit = false };
            }
        }

        return new DetailPageResult
        {
            ShouldExit = true,
            Action = action
        };
    }

    private static DetailPageResult PromptEditFields(IDetailPageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        // Get all editable components
        var editableComponents = page.Sections
            .Where(s => s.IsVisible && (!s.IsCollapsible || s.IsExpanded))
            .SelectMany(s => s.AllComponents)
            .Where(c => c.IsVisible && !c.IsReadOnly)
            .ToList();

        if (editableComponents.Count == 0)
        {
            console.MarkupLine($"[{theme.Colors.Warning}]No editable fields[/]");
            return new DetailPageResult { ShouldExit = false };
        }

        // Prompt which field to edit
        var fieldChoices = editableComponents
            .Select(c => (Component: (IComponentModel?)c, Label: c.Label ?? c.Id))
            .ToList();

        fieldChoices.Add((Component: null, Label: "[Done editing]"));

        var fieldPrompt = new SelectionPrompt<(IComponentModel? Component, string Label)>()
            .Title($"[{theme.Colors.Primary}]Select field to edit:[/]")
            .AddChoices(fieldChoices)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(theme.Colors.Selected));

        var selectedField = console.Prompt(fieldPrompt);

        if (selectedField.Component == null)
        {
            return new DetailPageResult { ShouldExit = false };
        }

        return new DetailPageResult
        {
            ShouldExit = false,
            EditComponent = selectedField.Component
        };
    }
}