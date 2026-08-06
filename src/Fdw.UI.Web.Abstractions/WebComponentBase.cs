using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.UI.Abstractions;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// CRTP-based web component that can render to ANY JavaScript framework.
/// Exports metadata that can be consumed by Blazor, React, Vue, Angular, etc.
/// </summary>
public abstract class WebComponentBase<TSelf, TModel> : ComponentBase<TSelf, TModel>, IWebComponent
    where TSelf : WebComponentBase<TSelf, TModel>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// JavaScript interop implementation (set by framework).
    /// </summary>
    public IJavaScriptInterop? JSInterop { get; set; }

    /// <summary>
    /// Render mode ID (reference to RenderModes TypeCollection).
    /// </summary>
    public int RenderModeId { get; set; } = 1; // Default: View

    /// <summary>
    /// Gets metadata that ANY JavaScript framework can understand.
    /// </summary>
    public new virtual ComponentMetadata GetMetadata()
    {
        return new ComponentMetadata
        {
            ComponentType = typeof(TSelf).Name,
            ModelType = typeof(TModel).Name,
            Properties = GetPropertyMetadata(),
            ChildComponents = GetChildComponents()
                .Select(c => c.GetMetadata())
                .ToList(),
            RenderModeId = RenderModeId
        };
    }

    /// <summary>
    /// Serializes component to JSON for JavaScript consumption.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(GetMetadata(), JsonOptions);
    }

    /// <summary>
    /// Renders component to HTML string (framework-agnostic).
    /// </summary>
    public virtual string RenderToHtml()
    {
        var metadata = GetMetadata();
        return HtmlRenderer.Render(metadata);
    }

    /// <summary>
    /// Gets property metadata for all properties.
    /// Must be implemented by derived classes.
    /// </summary>
    public abstract IList<PropertyMetadata> GetPropertyMetadata();

    /// <summary>
    /// Gets child components (for nested structures).
    /// Must be implemented by derived classes.
    /// </summary>
    public abstract IList<IWebComponent> GetChildComponents();
}
