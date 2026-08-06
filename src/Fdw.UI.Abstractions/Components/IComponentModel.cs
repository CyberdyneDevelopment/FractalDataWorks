using System;

namespace Fdw.UI.Abstractions.Components;

/// <summary>
/// Base interface for all UI component data models.
/// </summary>
/// <remarks>
/// <para>
/// Component models represent the data and metadata for UI components,
/// independent of any specific rendering framework. The same model can
/// be rendered by Spectre.Console, RazorConsole, Blazor, or future backends.
/// </para>
/// <para>
/// Implementations should be pure data objects without framework-specific dependencies.
/// </para>
/// </remarks>
public interface IComponentModel
{
    /// <summary>
    /// Gets the unique identifier for this component instance.
    /// </summary>
    /// <remarks>
    /// Used for form binding, validation messages, and accessibility.
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the display label for this component.
    /// </summary>
    string? Label { get; }

    /// <summary>
    /// Gets the help text displayed to assist users.
    /// </summary>
    string? HelpText { get; }

    /// <summary>
    /// Gets a value indicating whether this component requires a value.
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Gets a value indicating whether this component is read-only.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether this component is visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets the display order of this component.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Validates the current state of this component.
    /// </summary>
    /// <returns>A validation result indicating success or failure with messages.</returns>
    ValidationResult Validate();
}