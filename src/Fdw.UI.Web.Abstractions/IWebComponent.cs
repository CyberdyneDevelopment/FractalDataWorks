using System.Collections.Generic;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Base interface for web components that can export metadata.
/// </summary>
public interface IWebComponent
{
    /// <summary>
    /// Gets component metadata for serialization.
    /// </summary>
    ComponentMetadata GetMetadata();

    /// <summary>
    /// Serializes component to JSON.
    /// </summary>
    string ToJson();

    /// <summary>
    /// Renders component to HTML string.
    /// </summary>
    string RenderToHtml();

    /// <summary>
    /// Gets property metadata.
    /// </summary>
    IList<PropertyMetadata> GetPropertyMetadata();

    /// <summary>
    /// Gets child components.
    /// </summary>
    IList<IWebComponent> GetChildComponents();
}
