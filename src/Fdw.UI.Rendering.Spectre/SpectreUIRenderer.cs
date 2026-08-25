using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Abstractions.Rendering;
using IPageActionType = Fdw.UI.Abstractions.Rendering.IPageActionType;
using Fdw.UI.Components.Models;

// Only import PageModes from UI.Components.Pages, not the whole namespace (to avoid PageAction conflict)
using PageModes = Fdw.UI.Components.Pages.PageModes;
using Fdw.UI.Components.TypeIntegration;
using Fdw.UI.Rendering.Spectre.Messages;
using Fdw.UI.Rendering.Spectre.PageRenderers;
using Fdw.UI.Rendering.Spectre.Results;
using Fdw.UI.Themes;
using Microsoft.Extensions.Logging;
using Spectre.Console;

// Alias to avoid conflict with Fdw.UI.Abstractions.Components.ValidationResult
using SpectreValidation = Spectre.Console.ValidationResult;

namespace Fdw.UI.Rendering.Spectre;

/// <summary>
/// Spectre.Console implementation of <see cref="IUIRenderer"/>.
/// </summary>
public sealed class SpectreUIRenderer : IUIRenderer
{
    private readonly ILogger<SpectreUIRenderer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreUIRenderer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SpectreUIRenderer(ILogger<SpectreUIRenderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool SupportsInteractiveMode => true;

    /// <inheritdoc />
    public bool SupportsAnsiColors => true;

    /// <inheritdoc />
    public bool SupportsFocusManagement => false;

    /// <inheritdoc />
    public bool SupportsHotReload => false;

    /// <inheritdoc />
    public Task<RenderResult> Render(
        IComponentModel model,
        IRenderContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is not SpectreRenderContext spectreContext)
        {
            return Task.FromResult(RenderResult.Failure(SpectreUIResultCodes.ByName("InvalidRenderContext").MessageTemplate));
        }

        try
        {
            SpectreRenderingMessages.RenderingComponent(_logger, model.Id, model.GetType().Name);

            RenderComponent(model, spectreContext);
            return Task.FromResult(RenderResult.Ok());
        }
        catch (Exception ex)
        {
            SpectreRenderingMessages.RenderError(_logger, model.Id, ex.Message);
            return Task.FromResult(RenderResult.Failure($"Render failed: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<PromptResult<T>> Prompt<T>(
        IInputComponentModel<T> model,
        IRenderContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is not SpectreRenderContext spectreContext)
        {
            return Task.FromResult(PromptResult<T>.Failure(SpectreUIResultCodes.ByName("InvalidRenderContext").MessageTemplate));
        }

        try
        {
            SpectreRenderingMessages.PromptingUser(_logger, model.Id, model.GetType().Name);

            var value = PromptForValue(model, spectreContext);
            return Task.FromResult(PromptResult<T>.Ok(value));
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            SpectreRenderingMessages.UserCancelled(_logger, model.Id);
            return Task.FromResult(PromptResult<T>.Cancel());
        }
        catch (Exception ex)
        {
            SpectreRenderingMessages.RenderError(_logger, model.Id, ex.Message);
            return Task.FromResult(PromptResult<T>.Failure($"Prompt failed: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<ListPageResult> RenderListPage(
        IListPageModel page,
        IRenderContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is not SpectreRenderContext spectreContext)
        {
            return Task.FromResult(ListPageResult.Failure(SpectreUIResultCodes.ByName("InvalidRenderContext").MessageTemplate));
        }

        try
        {
            // Why: ListPageRenderer is an implementation detail of THIS renderer, not something a caller
            // reaches for. Callers hold an IUIRenderer resolved from UIRenderers and never name Spectre.
            return Task.FromResult(ListPageRenderer.Render(page, spectreContext));
        }
        catch (Exception ex)
        {
            SpectreRenderingMessages.RenderError(_logger, page.Id, ex.Message);
            return Task.FromResult(ListPageResult.Failure($"List page render failed: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<PageResult> RenderPage(
        IPageModel page,
        IRenderContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is not SpectreRenderContext spectreContext)
        {
            return Task.FromResult(PageResult.Failure(SpectreUIResultCodes.ByName("InvalidRenderContext").MessageTemplate));
        }

        try
        {
            SpectreRenderingMessages.ThemeApplied(_logger, spectreContext.Theme.Name, spectreContext.Theme.Colors.Name);

            // Render page header
            RenderPageHeader(page, spectreContext);

            // Render each section
            foreach (var section in page.Sections.Where(s => s.IsVisible))
            {
                RenderSection(section, spectreContext);
            }

            // Prompt for action
            var action = PromptForPageAction(page, spectreContext);

            SpectreRenderingMessages.PageRendered(_logger, page.Title, page.Sections.Count);

            return Task.FromResult(action.Name switch
            {
                "Save" => HandleSave(page, spectreContext),
                "Delete" => HandleDelete(page, spectreContext),
                "Cancel" => PageResult.Cancel(),
                _ => PageResult.Cancel()
            });
        }
        catch (Exception ex)
        {
            SpectreRenderingMessages.RenderError(_logger, page.Id, ex.Message);
            return Task.FromResult(PageResult.Failure($"Page render failed: {ex.Message}"));
        }
    }

    private void RenderComponent(IComponentModel model, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        switch (model)
        {
            case TextInputModel textInput:
                RenderTextInput(textInput, context);
                break;

            case CheckboxModel checkbox:
                RenderCheckbox(checkbox, context);
                break;

            case DatePickerModel datePicker:
                RenderDatePicker(datePicker, context);
                break;

            case TypeCollectionSelectModel typeSelect:
                RenderTypeCollectionSelect(typeSelect, context);
                break;

            case IInputComponentModel inputModel when IsNumericModel(inputModel):
                RenderNumericInput(inputModel, context);
                break;

            case ISelectableComponentModel selectModel:
                RenderSelect(selectModel, context);
                break;

            case IMultiSelectComponentModel multiSelect:
                RenderMultiSelect(multiSelect, context);
                break;

            default:
                SpectreRenderingMessages.UnsupportedComponentType(_logger, model.GetType().Name, model.Id);
                console.MarkupLine($"[{theme.Colors.Muted}]Unsupported component type: {model.GetType().Name}[/]");
                break;
        }
    }

    private static bool IsNumericModel(IInputComponentModel model)
    {
        var valueType = model.ValueType;
        return valueType == typeof(int) || valueType == typeof(long) ||
               valueType == typeof(decimal) || valueType == typeof(double) ||
               valueType == typeof(float) || valueType == typeof(short) ||
               valueType == typeof(byte);
    }

    private static void RenderTextInput(TextInputModel model, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        var labelColor = model.IsRequired ? theme.Colors.Primary : theme.Colors.Foreground;
        var requiredMarker = model.IsRequired ? $"[{theme.Colors.Error}]*[/]" : "";

        console.MarkupLine($"[{labelColor}]{model.Label ?? model.Id}[/]{requiredMarker}");

        if (!string.IsNullOrEmpty(model.HelpText))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]{model.HelpText}[/]");
        }

        if (string.Equals(context.Mode.Name, "Display", System.StringComparison.Ordinal) || model.IsReadOnly)
        {
            console.MarkupLine($"  [{theme.Colors.Foreground}]{model.Value ?? "(empty)"}[/]");
        }
    }

    private static void RenderCheckbox(CheckboxModel model, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        var icon = model.Value == true ? theme.Icons.CheckedIndicator : theme.Icons.UncheckedIndicator;
        console.MarkupLine($"[{theme.Colors.Foreground}]{icon} {model.Label ?? model.Id}[/]");

        if (!string.IsNullOrEmpty(model.HelpText))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]  {model.HelpText}[/]");
        }
    }

    private static void RenderNumericInput(IInputComponentModel model, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        var labelColor = model.IsRequired ? theme.Colors.Primary : theme.Colors.Foreground;
        var requiredMarker = model.IsRequired ? $"[{theme.Colors.Error}]*[/]" : "";

        console.MarkupLine($"[{labelColor}]{model.Label ?? model.Id}[/]{requiredMarker}");

        if (!string.IsNullOrEmpty(model.HelpText))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]{model.HelpText}[/]");
        }

        if (string.Equals(context.Mode.Name, "Display", System.StringComparison.Ordinal) || model.IsReadOnly)
        {
            var displayValue = model.ValueAsObject?.ToString() ?? "(empty)";
            console.MarkupLine($"  [{theme.Colors.Foreground}]{displayValue}[/]");
        }
    }

    private static void RenderDatePicker(DatePickerModel model, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        var labelColor = model.IsRequired ? theme.Colors.Primary : theme.Colors.Foreground;
        var requiredMarker = model.IsRequired ? $"[{theme.Colors.Error}]*[/]" : "";

        console.MarkupLine($"[{labelColor}]{model.Label ?? model.Id}[/]{requiredMarker}");

        if (!string.IsNullOrEmpty(model.HelpText))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]{model.HelpText}[/]");
        }

        if (string.Equals(context.Mode.Name, "Display", System.StringComparison.Ordinal) || model.IsReadOnly)
        {
            var displayValue = model.Value?.ToString(model.Format, CultureInfo.InvariantCulture) ?? "(empty)";
            console.MarkupLine($"  [{theme.Colors.Foreground}]{displayValue}[/]");
        }
    }

    private static void RenderSelect(ISelectableComponentModel model, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        var labelColor = model.IsRequired ? theme.Colors.Primary : theme.Colors.Foreground;
        var requiredMarker = model.IsRequired ? $"[{theme.Colors.Error}]*[/]" : "";

        console.MarkupLine($"[{labelColor}]{model.Label ?? model.Id}[/]{requiredMarker}");

        if (!string.IsNullOrEmpty(model.HelpText))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]{model.HelpText}[/]");
        }

        if (string.Equals(context.Mode.Name, "Display", System.StringComparison.Ordinal) || model.IsReadOnly)
        {
            var selectedValue = (model as IInputComponentModel)?.ValueAsObject;
            var displayText = GetSelectDisplayText(model, selectedValue) ?? "(none)";
            console.MarkupLine($"  [{theme.Colors.Foreground}]{displayText}[/]");
        }
    }

    private static void RenderTypeCollectionSelect(TypeCollectionSelectModel model, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        var labelColor = model.IsRequired ? theme.Colors.Primary : theme.Colors.Foreground;
        var requiredMarker = model.IsRequired ? $"[{theme.Colors.Error}]*[/]" : "";
        var typeCollectionHint = !string.IsNullOrEmpty(model.TypeCollectionName)
            ? $" [dim]({model.TypeCollectionName})[/]"
            : "";

        console.MarkupLine($"[{labelColor}]{model.Label ?? model.Id}[/]{requiredMarker}{typeCollectionHint}");

        if (!string.IsNullOrEmpty(model.HelpText))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]{model.HelpText}[/]");
        }

        if (string.Equals(context.Mode.Name, "Display", System.StringComparison.Ordinal) || model.IsReadOnly)
        {
            var selectedOption = model.Value.HasValue
                ? model.Options.FirstOrDefault(o => o.Value == model.Value.Value)
                : null;
            var displayText = selectedOption?.DisplayText ?? model.EmptyOptionText;
            console.MarkupLine($"  [{theme.Colors.Foreground}]{displayText}[/]");
        }
    }

    private static void RenderMultiSelect(IMultiSelectComponentModel model, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        var labelColor = model.IsRequired ? theme.Colors.Primary : theme.Colors.Foreground;
        var requiredMarker = model.IsRequired ? $"[{theme.Colors.Error}]*[/]" : "";

        console.MarkupLine($"[{labelColor}]{model.Label ?? model.Id}[/]{requiredMarker}");

        if (!string.IsNullOrEmpty(model.HelpText))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]{model.HelpText}[/]");
        }

        if (string.Equals(context.Mode.Name, "Display", System.StringComparison.Ordinal) || model.IsReadOnly)
        {
            if (model.SelectedCount == 0)
            {
                console.MarkupLine($"  [{theme.Colors.Muted}](none selected)[/]");
            }
            else
            {
                console.MarkupLine($"  [{theme.Colors.Info}]{model.SelectedCount} item(s) selected[/]");
            }
        }
    }

    private static string? GetSelectDisplayText(ISelectableComponentModel model, object? selectedValue)
    {
        if (selectedValue == null) return null;

        // Try to find the matching option by iterating
        var optionsProperty = model.GetType().GetProperty("Options");
        if (optionsProperty?.GetValue(model) is System.Collections.IEnumerable options)
        {
            foreach (var opt in options)
            {
                var valueProp = opt.GetType().GetProperty("Value");
                var displayProp = opt.GetType().GetProperty("DisplayText");
                if (valueProp != null && displayProp != null)
                {
                    var optValue = valueProp.GetValue(opt);
                    if (Equals(optValue, selectedValue))
                    {
                        return displayProp.GetValue(opt)?.ToString();
                    }
                }
            }
        }

        return selectedValue.ToString();
    }

    private static T PromptForValue<T>(IInputComponentModel<T> model, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        // Handle different model types
        if (model is TextInputModel textModel && typeof(T) == typeof(string))
        {
            return (T)(object)PromptTextInput(textModel, console, theme);
        }

        if (model is CheckboxModel checkModel && typeof(T) == typeof(bool))
        {
            var result = console.Confirm($"[{theme.Colors.Primary}]{checkModel.Label ?? checkModel.Id}[/]", checkModel.Value ?? false);
            return (T)(object)result;
        }

        if (model is DatePickerModel dateModel && typeof(T) == typeof(DateTime))
        {
            return (T)(object)PromptDatePicker(dateModel, console, theme);
        }

        if (model is TypeCollectionSelectModel typeSelectModel && typeof(T) == typeof(int))
        {
            return (T)(object)PromptTypeCollectionSelect(typeSelectModel, console, theme);
        }

        // Handle generic SelectModel<T>
        if (model is ISelectableComponentModel selectModel)
        {
            return PromptSelect<T>(selectModel, console, theme);
        }

        // Handle numeric types
        if (IsNumericModel(model))
        {
            return PromptNumeric<T>(model, console, theme);
        }

        throw new NotSupportedException($"Prompting for type {typeof(T).Name} with model {model.GetType().Name} is not supported.");
    }

    private static string PromptTextInput(TextInputModel model, IAnsiConsole console, IMenuTheme theme)
    {
        var prompt = new TextPrompt<string>($"[{theme.Colors.Primary}]{model.Label ?? model.Id}:[/]");
        prompt.AllowEmpty = !model.IsRequired;

        if (!string.IsNullOrEmpty(model.DefaultValue))
        {
            prompt.DefaultValue(model.DefaultValue);
        }

        if (model.IsPassword)
        {
            prompt.Secret();
        }

        // Add validation for max length
        if (model.MaxLength.HasValue)
        {
            var maxLen = model.MaxLength.Value;
            prompt.Validate(value =>
            {
                if (value.Length > maxLen)
                {
                    return SpectreValidation.Error($"[{theme.Colors.Error}]Maximum length is {maxLen}[/]");
                }
                return SpectreValidation.Success();
            });
        }

        // Add validation for pattern
        if (!string.IsNullOrEmpty(model.Pattern))
        {
            var regex = new System.Text.RegularExpressions.Regex(
                model.Pattern,
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromSeconds(1));
            prompt.Validate(value =>
            {
                if (!string.IsNullOrEmpty(value) && !regex.IsMatch(value))
                {
                    return SpectreValidation.Error($"[{theme.Colors.Error}]Invalid format[/]");
                }
                return SpectreValidation.Success();
            });
        }

        return console.Prompt(prompt);
    }

    private static DateTime PromptDatePicker(DatePickerModel model, IAnsiConsole console, IMenuTheme theme)
    {
        var format = model.IncludeTime ? $"{model.Format} {model.TimeFormat}" : model.Format;
        var promptText = $"[{theme.Colors.Primary}]{model.Label ?? model.Id} ({format}):[/]";

        var prompt = new TextPrompt<string>(promptText);
        prompt.AllowEmpty = !model.IsRequired;

        if (model.DefaultValue.HasValue)
        {
            prompt.DefaultValue(model.DefaultValue.Value.ToString(format, CultureInfo.InvariantCulture));
        }

        prompt.Validate(input =>
        {
            if (string.IsNullOrEmpty(input) && !model.IsRequired)
            {
                return SpectreValidation.Success();
            }

            if (!DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return SpectreValidation.Error($"[{theme.Colors.Error}]Invalid date format. Use {format}[/]");
            }

            if (model.MinDate.HasValue && parsed < model.MinDate.Value)
            {
                return SpectreValidation.Error($"[{theme.Colors.Error}]Date must be on or after {model.MinDate.Value.ToString(model.Format, CultureInfo.InvariantCulture)}[/]");
            }

            if (model.MaxDate.HasValue && parsed > model.MaxDate.Value)
            {
                return SpectreValidation.Error($"[{theme.Colors.Error}]Date must be on or before {model.MaxDate.Value.ToString(model.Format, CultureInfo.InvariantCulture)}[/]");
            }

            return SpectreValidation.Success();
        });

        var result = console.Prompt(prompt);
        return DateTime.ParseExact(result, format, CultureInfo.InvariantCulture);
    }

    private static int PromptTypeCollectionSelect(TypeCollectionSelectModel model, IAnsiConsole console, IMenuTheme theme)
    {
        var options = model.Options.Where(o => !o.IsDisabled).ToList();

        if (options.Count == 0)
        {
            console.MarkupLine($"[{theme.Colors.Warning}]No options available[/]");
            return model.DefaultValue ?? 0;
        }

        var prompt = new SelectionPrompt<SelectOption<int>>()
            .Title($"[{theme.Colors.Primary}]{model.Label ?? model.Id}:[/]")
            .AddChoices(options)
            .UseConverter(opt => opt.DisplayText)
            .HighlightStyle(new Style(theme.Colors.Selected));

        if (model.Value.HasValue)
        {
            var currentOption = options.FirstOrDefault(o => o.Value == model.Value.Value);
            if (currentOption != null)
            {
                // Note: SelectionPrompt doesn't have a native way to set default,
                // but the first matching item is shown first
            }
        }

        var selected = console.Prompt(prompt);
        return selected.Value;
    }

    private static T PromptSelect<T>(ISelectableComponentModel model, IAnsiConsole console, IMenuTheme theme)
    {
        // Get options through reflection since we don't know T at compile time
        var optionsProperty = model.GetType().GetProperty("Options");
        var options = optionsProperty?.GetValue(model) as System.Collections.IEnumerable;

        if (options == null)
        {
            throw new InvalidOperationException("Cannot retrieve options from select model");
        }

        var optionList = new List<(T Value, string Display, bool IsDisabled)>();
        foreach (var opt in options)
        {
            var valueProp = opt.GetType().GetProperty("Value");
            var displayProp = opt.GetType().GetProperty("DisplayText");
            var disabledProp = opt.GetType().GetProperty("IsDisabled");

            if (valueProp != null && displayProp != null)
            {
                var value = (T)valueProp.GetValue(opt)!;
                var display = displayProp.GetValue(opt)?.ToString() ?? value?.ToString() ?? "";
                var isDisabled = disabledProp?.GetValue(opt) as bool? ?? false;

                if (!isDisabled)
                {
                    optionList.Add((value, display, isDisabled));
                }
            }
        }

        if (optionList.Count == 0)
        {
            console.MarkupLine($"[{theme.Colors.Warning}]No options available[/]");
            return default!;
        }

        var prompt = new SelectionPrompt<(T Value, string Display, bool IsDisabled)>()
            .Title($"[{theme.Colors.Primary}]{model.Label ?? model.Id}:[/]")
            .AddChoices(optionList)
            .UseConverter(opt => opt.Display)
            .HighlightStyle(new Style(theme.Colors.Selected));

        var selected = console.Prompt(prompt);
        return selected.Value;
    }

    private static T PromptNumeric<T>(IInputComponentModel<T> model, IAnsiConsole console, IMenuTheme theme)
    {
        var promptText = $"[{theme.Colors.Primary}]{model.Label ?? model.Id}:[/]";
        var prompt = new TextPrompt<T>(promptText);

        // Get default value if available
        var defaultValueProp = model.GetType().GetProperty("DefaultValue");
        var defaultValue = defaultValueProp?.GetValue(model);
        if (defaultValue != null)
        {
            prompt.DefaultValue((T)defaultValue);
        }

        // Get min/max for validation
        var minProp = model.GetType().GetProperty("MinValue");
        var maxProp = model.GetType().GetProperty("MaxValue");
        var minValue = minProp?.GetValue(model);
        var maxValue = maxProp?.GetValue(model);

        prompt.Validate(value =>
        {
            if (minValue != null && Comparer<T>.Default.Compare(value, (T)minValue) < 0)
            {
                return SpectreValidation.Error($"[{theme.Colors.Error}]Value must be at least {minValue}[/]");
            }

            if (maxValue != null && Comparer<T>.Default.Compare(value, (T)maxValue) > 0)
            {
                return SpectreValidation.Error($"[{theme.Colors.Error}]Value must be at most {maxValue}[/]");
            }

            return SpectreValidation.Success();
        });

        return console.Prompt(prompt);
    }

    private static void RenderPageHeader(IPageModel page, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        var title = page.Mode.GetTitlePrefix(page.Title);

        var panel = new Panel(new Markup($"[{theme.Colors.Foreground}]{page.Description ?? ""}[/]"))
        {
            Header = new PanelHeader($"[{theme.Colors.Primary} bold]{title}[/]"),
            Border = theme.Borders.Panel,
            BorderStyle = new Style(theme.Colors.Primary)
        };

        console.Write(panel);
        console.WriteLine();
    }

    private void RenderSection(ISectionModel section, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        if (section.IsCollapsible && !section.IsExpanded)
        {
            console.MarkupLine($"[{theme.Colors.Muted}]{theme.Icons.UnselectedIndicator} {section.Title ?? section.Id} (collapsed)[/]");
            return;
        }

        if (!string.IsNullOrEmpty(section.Title))
        {
            console.MarkupLine($"[{theme.Colors.Secondary} bold]{section.Title}[/]");
            if (!string.IsNullOrEmpty(section.Description))
            {
                console.MarkupLine($"[{theme.Colors.Muted}]{section.Description}[/]");
            }
            console.WriteLine();
        }

        foreach (var component in section.AllComponents.Where(c => c.IsVisible))
        {
            RenderComponent(component, context);
        }

        console.WriteLine();
    }

    private static IPageActionType PromptForPageAction(IPageModel page, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        var choices = new List<string> { "Save", "Cancel" };
        if (page.Mode.IsEditable && !page.Mode.IsCreateMode)
        {
            choices.Add("Delete");
        }

        var selection = console.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{theme.Colors.Primary}]Choose action:[/]")
                .AddChoices(choices)
                .HighlightStyle(new Style(theme.Colors.Selected)));

        return selection switch
        {
            "Save" => PageActions.Save,
            "Delete" => PageActions.Delete,
            _ => PageActions.Cancel
        };
    }

    private PageResult HandleSave(IPageModel page, SpectreRenderContext context)
    {
        var validation = page.Validate();
        if (!validation.IsValid)
        {
            foreach (var error in validation.Messages.Where(m => string.Equals(m.Severity.Name, "Error", System.StringComparison.Ordinal)))
            {
                SpectreRenderingMessages.ValidationFailed(_logger, page.Id, error.Message);
                context.Console.MarkupLine($"[{context.Theme.Colors.Error}]{context.Theme.Icons.ErrorIcon} {error.Message}[/]");
            }
            return PageResult.ValidationFailed(validation);
        }

        SpectreRenderingMessages.ConfigurationSaved(_logger, page.Title);
        return PageResult.Save(page);
    }

    private PageResult HandleDelete(IPageModel page, SpectreRenderContext context)
    {
        var confirmed = context.Console.Confirm(
            $"[{context.Theme.Colors.Warning}]Are you sure you want to delete '{page.Title}'?[/]",
            false);

        if (!confirmed)
        {
            return PageResult.Cancel();
        }

        SpectreRenderingMessages.DeletionRequested(_logger, page.Title);
        return PageResult.Delete();
    }
}
