using System;
using System.Linq;
using System.Text;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Renders component metadata to HTML string.
/// </summary>
public static class HtmlRenderer
{
    /// <summary>
    /// Renders component metadata to HTML.
    /// </summary>
    /// <param name="metadata">The component metadata to render.</param>
    /// <returns>HTML string representation of the component.</returns>
    public static string Render(ComponentMetadata metadata)
    {
        var html = new StringBuilder();
        html.Append($"<div class=\"component {metadata.ComponentType.ToLowerInvariant()}\">");
        html.Append($"{Environment.NewLine}");

        foreach (var property in metadata.Properties.OrderBy(p => p.DisplayOrder))
        {
            html.Append($"{RenderProperty(property)}");
        }

        foreach (var child in metadata.ChildComponents)
        {
            html.Append($"{Render(child)}");
        }

        html.Append($"</div>{Environment.NewLine}");
        return html.ToString();
    }

    private static string RenderProperty(PropertyMetadata property)
    {
        var componentType = ComponentTypes.ByName(property.ComponentType);

        if (componentType == null)
        {
            return RenderGenericInput(property);
        }

        // Use TypeCollection to determine rendering
        return componentType.Id switch
        {
            1 => RenderTextInput(property),        // TextInput
            2 => RenderNumericInput(property),     // NumericInput
            3 => RenderTextArea(property),         // TextArea
            4 => RenderSwitch(property),           // Switch
            5 => RenderDatePicker(property),       // DatePicker
            10 => RenderDropdown(property),        // Dropdown
            _ => RenderGenericInput(property)
        };
    }

    private static string RenderTextInput(PropertyMetadata property)
    {
        var html = new StringBuilder();
        html.Append($"  <div class=\"property-wrapper\">{Environment.NewLine}");

        if (!string.IsNullOrEmpty(property.Label))
        {
            html.Append($"    <label>{property.Label}</label>{Environment.NewLine}");
        }

        html.Append($"    <input type=\"text\" value=\"{property.Value}\"");

        if (property.Required)
        {
            html.Append(" required");
        }

        if (property.ReadOnly)
        {
            html.Append(" readonly");
        }

        if (!string.IsNullOrEmpty(property.Placeholder))
        {
            html.Append($" placeholder=\"{property.Placeholder}\"");
        }

        if (property.ValidationRules.TryGetValue("pattern", out var pattern))
        {
            html.Append($" pattern=\"{pattern}\"");
        }

        html.Append($" />{Environment.NewLine}");

        if (!string.IsNullOrEmpty(property.HelpText))
        {
            html.Append($"    <small>{property.HelpText}</small>{Environment.NewLine}");
        }

        html.Append($"  </div>{Environment.NewLine}");
        return html.ToString();
    }

    private static string RenderNumericInput(PropertyMetadata property)
    {
        var html = new StringBuilder();
        html.Append($"  <div class=\"property-wrapper\">{Environment.NewLine}");

        if (!string.IsNullOrEmpty(property.Label))
        {
            html.Append($"    <label>{property.Label}</label>{Environment.NewLine}");
        }

        html.Append($"    <input type=\"number\" value=\"{property.Value}\"");

        if (property.ValidationRules.TryGetValue("min", out var min))
        {
            html.Append($" min=\"{min}\"");
        }

        if (property.ValidationRules.TryGetValue("max", out var max))
        {
            html.Append($" max=\"{max}\"");
        }

        if (property.ReadOnly)
        {
            html.Append(" readonly");
        }

        html.Append($" />{Environment.NewLine}");

        if (!string.IsNullOrEmpty(property.HelpText))
        {
            html.Append($"    <small>{property.HelpText}</small>{Environment.NewLine}");
        }

        html.Append($"  </div>{Environment.NewLine}");
        return html.ToString();
    }

    private static string RenderTextArea(PropertyMetadata property)
    {
        var html = new StringBuilder();
        html.Append($"  <div class=\"property-wrapper\">{Environment.NewLine}");

        if (!string.IsNullOrEmpty(property.Label))
        {
            html.Append($"    <label>{property.Label}</label>{Environment.NewLine}");
        }

        html.Append("    <textarea");

        if (property.ReadOnly)
        {
            html.Append(" readonly");
        }

        html.Append($">{property.Value}</textarea>{Environment.NewLine}");

        html.Append($"  </div>{Environment.NewLine}");
        return html.ToString();
    }

    private static string RenderSwitch(PropertyMetadata property)
    {
        var html = new StringBuilder();
        html.Append($"  <div class=\"property-wrapper\">{Environment.NewLine}");

        html.Append($"    <input type=\"checkbox\" value=\"{property.Value}\"");

        if (property.Value is bool boolValue && boolValue)
        {
            html.Append(" checked");
        }

        if (property.ReadOnly)
        {
            html.Append(" disabled");
        }

        html.Append($" />{Environment.NewLine}");

        if (!string.IsNullOrEmpty(property.Label))
        {
            html.Append($"    <label>{property.Label}</label>{Environment.NewLine}");
        }

        html.Append($"  </div>{Environment.NewLine}");
        return html.ToString();
    }

    private static string RenderDatePicker(PropertyMetadata property)
    {
        var html = new StringBuilder();
        html.Append($"  <div class=\"property-wrapper\">{Environment.NewLine}");

        if (!string.IsNullOrEmpty(property.Label))
        {
            html.Append($"    <label>{property.Label}</label>{Environment.NewLine}");
        }

        html.Append($"    <input type=\"date\" value=\"{property.Value}\"");

        if (property.ReadOnly)
        {
            html.Append(" readonly");
        }

        html.Append($" />{Environment.NewLine}");

        html.Append($"  </div>{Environment.NewLine}");
        return html.ToString();
    }

    private static string RenderDropdown(PropertyMetadata property)
    {
        var html = new StringBuilder();
        html.Append($"  <div class=\"property-wrapper\">{Environment.NewLine}");

        if (!string.IsNullOrEmpty(property.Label))
        {
            html.Append($"    <label>{property.Label}</label>{Environment.NewLine}");
        }

        html.Append($"    <select>{Environment.NewLine}");

        if (property.Attributes.ContainsKey("options"))
        {
            // Options would be rendered here
            html.Append($"      <option>-- Select --</option>{Environment.NewLine}");
        }

        html.Append($"    </select>{Environment.NewLine}");
        html.Append($"  </div>{Environment.NewLine}");
        return html.ToString();
    }

    private static string RenderGenericInput(PropertyMetadata property)
    {
        var html = new StringBuilder();
        html.Append($"  <div class=\"property-wrapper\">{Environment.NewLine}");

        if (!string.IsNullOrEmpty(property.Label))
        {
            html.Append($"    <label>{property.Label}</label>{Environment.NewLine}");
        }

        html.Append($"    <input type=\"text\" value=\"{property.Value}\" />{Environment.NewLine}");
        html.Append($"  </div>{Environment.NewLine}");
        return html.ToString();
    }
}
