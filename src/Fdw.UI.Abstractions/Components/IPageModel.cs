using System.Collections.Generic;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Abstractions.Components;

/// <summary>
/// Represents a full page with sections.
/// </summary>
/// <remarks>
/// <para>
/// A page is the top-level container for a configuration form.
/// It contains sections, which contain columns, which contain components.
/// </para>
/// <para>
/// The page structure is independent of the rendering framework.
/// The same IPageModel can be rendered by Spectre.Console, RazorConsole, or Blazor.
/// </para>
/// </remarks>
public interface IPageModel
{
    /// <summary>
    /// Gets the unique identifier for this page.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the page title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the page description.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the sections in this page.
    /// </summary>
    IReadOnlyList<ISectionModel> Sections { get; }

    /// <summary>
    /// Gets the current page mode (View, Create, Edit).
    /// </summary>
    IPageMode Mode { get; }

    /// <summary>
    /// Gets a value indicating whether the page has unsaved changes.
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// Validates all components in the page.
    /// </summary>
    /// <returns>A combined validation result.</returns>
    ValidationResult Validate();
}