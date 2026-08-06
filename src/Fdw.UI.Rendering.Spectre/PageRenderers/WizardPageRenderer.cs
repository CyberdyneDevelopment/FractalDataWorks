using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Renders wizard pages using Spectre.Console with step indicators and navigation.
/// </summary>
public sealed class WizardPageRenderer
{
    /// <summary>
    /// Renders a wizard page and returns the selected action.
    /// </summary>
    public static WizardPageResult Render(IWizardPageModel wizard, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        console.Clear();

        // Render header
        RenderHeader(wizard, console, theme);

        // Render step indicator
        RenderStepIndicator(wizard, console, theme);

        // Render current step content
        RenderStepContent(wizard.CurrentStep, console, theme);

        // Render navigation
        return RenderNavigation(wizard, console, theme);
    }

    private static void RenderHeader(IWizardPageModel wizard, IAnsiConsole console, IMenuTheme theme)
    {
        var rule = new Rule($"[{theme.Colors.Primary} bold]{wizard.Title}[/]")
        {
            Justification = Justify.Left,
            Style = new Style(theme.Colors.Primary)
        };
        console.Write(rule);

        if (!string.IsNullOrEmpty(wizard.Description))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]{wizard.Description}[/]");
        }

        console.WriteLine();
    }

    private static void RenderStepIndicator(IWizardPageModel wizard, IAnsiConsole console, IMenuTheme theme)
    {
        var stepParts = new List<string>();

        foreach (var step in wizard.Steps)
        {
            var icon = GetStepIcon(step, theme);
            var color = GetStepColor(step, wizard.CurrentStepIndex, theme);
            var label = step.Title;

            if (label.Length > 15)
            {
                label = string.Concat(label.AsSpan(0, 12), "...");
            }

            var optional = step.IsOptional ? " (opt)" : "";
            stepParts.Add($"[{color}]{icon} {step.StepNumber}. {label}{optional}[/]");
        }

        console.MarkupLine(string.Join($" [{theme.Colors.Muted}]→[/] ", stepParts));
        console.WriteLine();
    }

    private static string GetStepIcon(IWizardStep step, IMenuTheme theme)
    {
        return step.Status.Name switch
        {
            "Complete" => theme.Icons.SuccessIcon,
            "InProgress" => "►",
            "Error" => theme.Icons.ErrorIcon,
            "Skipped" => "○",
            _ => "○"
        };
    }

    private static Color GetStepColor(IWizardStep step, int currentIndex, IMenuTheme theme)
    {
        if (step.StepNumber - 1 == currentIndex)
        {
            return theme.Colors.Primary;
        }

        return step.Status.Name switch
        {
            "Complete" => theme.Colors.Success,
            "Error" => theme.Colors.Error,
            "Skipped" => theme.Colors.Muted,
            _ => theme.Colors.Muted
        };
    }

    private static void RenderStepContent(IWizardStep step, IAnsiConsole console, IMenuTheme theme)
    {
        // Step header
        var panel = new Panel(new Markup(
            $"[{theme.Colors.Foreground}]{step.Description ?? "Complete the fields below."}[/]"))
        {
            Header = new PanelHeader($"[{theme.Colors.Secondary} bold]Step {step.StepNumber}: {step.Title}[/]"),
            Border = theme.Borders.Panel,
            BorderStyle = new Style(theme.Colors.Secondary),
            Padding = new Padding(1, 0)
        };

        console.Write(panel);
        console.WriteLine();

        // Render step content (form fields)
        if (step.Content != null)
        {
            foreach (var section in step.Content.Sections.Where(s => s.IsVisible))
            {
                if (!string.IsNullOrEmpty(section.Title))
                {
                    console.MarkupLine($"[{theme.Colors.Secondary}]{section.Title}[/]");
                }

                // Simple field display - actual editing would be handled by the caller
                var grid = new Grid();
                grid.AddColumn(new GridColumn().Width(30));
                grid.AddColumn();

                foreach (var component in section.AllComponents.Where(c => c.IsVisible))
                {
                    var requiredMarker = component.IsRequired ? $"[{theme.Colors.Error}]*[/]" : "";
                    var label = $"[{theme.Colors.Foreground}]{component.Label ?? component.Id}[/]{requiredMarker}:";

                    var value = GetComponentDisplayValue(component, theme);
                    grid.AddRow(label, value);
                }

                console.Write(grid);
                console.WriteLine();
            }
        }

        // Show validation errors
        if (step.ValidationResult != null && !step.ValidationResult.IsValid)
        {
            console.MarkupLine($"[{theme.Colors.Error}]Validation errors:[/]");
            foreach (var error in step.ValidationResult.Messages.Where(m => string.Equals(m.Severity.Name, "Error", System.StringComparison.Ordinal)))
            {
                console.MarkupLine($"  [{theme.Colors.Error}]{theme.Icons.ErrorIcon} {Markup.Escape(error.Message)}[/]");
            }
            console.WriteLine();
        }
    }

    private static string GetComponentDisplayValue(Fdw.UI.Abstractions.Components.IComponentModel component, IMenuTheme theme)
    {
        if (component is Fdw.UI.Abstractions.Components.IInputComponentModel inputModel)
        {
            var value = inputModel.ValueAsObject;

            if (value == null)
            {
                return $"[{theme.Colors.Muted}](not set)[/]";
            }

            if (value is bool boolVal)
            {
                var icon = boolVal ? theme.Icons.CheckedIndicator : theme.Icons.UncheckedIndicator;
                return $"[{(boolVal ? theme.Colors.Success : theme.Colors.Muted)}]{icon}[/]";
            }

            // Hide sensitive values
            if (component.Id.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                component.Id.Contains("Secret", StringComparison.OrdinalIgnoreCase))
            {
                return $"[{theme.Colors.Muted}]********[/]";
            }

            return $"[{theme.Colors.Foreground}]{Markup.Escape(value.ToString() ?? "")}[/]";
        }

        return $"[{theme.Colors.Muted}]-[/]";
    }

    // MA0051: Method length acceptable - procedural wizard navigation with conditional menu construction
#pragma warning disable MA0051 // Method is too long
    private static WizardPageResult RenderNavigation(IWizardPageModel wizard, IAnsiConsole console, IMenuTheme theme)
#pragma warning restore MA0051
    {
        var choices = new List<(string Id, string Label)>();

        // Edit fields option
        if (wizard.CurrentStep.Content != null)
        {
            choices.Add(("edit", "[e] Edit Fields"));
        }

        // Navigation options
        if (wizard.CanGoBack)
        {
            choices.Add(("back", "[←] Previous Step"));
        }

        if (wizard.CanGoNext)
        {
            choices.Add(("next", "[→] Next Step"));
        }

        // Skip optional step
        if (wizard.AllowSkipOptional && wizard.CurrentStep.IsOptional && wizard.CanGoNext)
        {
            choices.Add(("skip", "[s] Skip This Step"));
        }

        // Complete wizard
        if (wizard.CanComplete)
        {
            choices.Add(("complete", $"[{theme.Colors.Success}][Enter] Complete Wizard[/]"));
        }

        // Cancel
        choices.Add(("cancel", "[q] Cancel Wizard"));

        var prompt = new SelectionPrompt<(string Id, string Label)>()
            .Title($"[{theme.Colors.Primary}]Navigation:[/]")
            .AddChoices(choices)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(theme.Colors.Selected));

        var selected = console.Prompt(prompt);

        switch (selected.Id)
        {
            case "edit":
                return new WizardPageResult { Action = WizardActions.EditFields };

            case "back":
                return new WizardPageResult { Action = WizardActions.Previous };

            case "next":
                return new WizardPageResult { Action = WizardActions.Next };

            case "skip":
                return new WizardPageResult { Action = WizardActions.Skip };

            case "complete":
                // Confirm completion
                if (!string.IsNullOrEmpty(wizard.CompletionSummary))
                {
                    console.MarkupLine($"[{theme.Colors.Info}]{wizard.CompletionSummary}[/]");
                }

                var confirmed = console.Confirm(
                    $"[{theme.Colors.Primary}]Complete this wizard?[/]",
                    true);

                return confirmed
                    ? new WizardPageResult { Action = WizardActions.Complete }
                    : new WizardPageResult { Action = WizardActions.None };

            case "cancel":
                var cancelConfirmed = console.Confirm(
                    $"[{theme.Colors.Warning}]Cancel wizard? All progress will be lost.[/]",
                    false);

                return cancelConfirmed
                    ? new WizardPageResult { Action = WizardActions.Cancel, ShouldExit = true }
                    : new WizardPageResult { Action = WizardActions.None };

            default:
                return new WizardPageResult { Action = WizardActions.None };
        }
    }
}