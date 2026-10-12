using System;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Components.Pages;

namespace Fdw.UI.Components.Configuration;

/// <summary>
/// Base class for configuration form models that wrap a configuration DTO for UI binding.
/// </summary>
/// <typeparam name="TConfiguration">The configuration DTO type.</typeparam>
/// <remarks>
/// <para>
/// Source generators create derived classes from this base for each [ManagedConfiguration] class.
/// The derived class provides strongly-typed component models for each property.
/// </para>
/// <para>
/// Example generated class:
/// <code>
/// public sealed class MsSqlConnectionConfigurationUIModel : ConfigurationFormModel&lt;MsSqlConnectionConfiguration&gt;
/// {
///     public TextInputModel Name { get; }
///     public TextInputModel Server { get; }
///     public NumericInputModel&lt;int&gt; Port { get; }
///     // ...
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class ConfigurationFormModel<TConfiguration>
    where TConfiguration : class, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationFormModel{TConfiguration}"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance to edit.</param>
    protected ConfigurationFormModel(TConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Gets the underlying configuration instance.
    /// </summary>
    protected TConfiguration Configuration { get; }

    /// <summary>
    /// Gets or sets the current page mode (View, Create, Edit).
    /// </summary>
    public IPageMode Mode { get; set; } = PageModes.View;

    /// <summary>
    /// Gets a value indicating whether the form has unsaved changes.
    /// </summary>
    public bool HasChanges { get; protected set; }

    /// <summary>
    /// Converts this UI model back to a configuration DTO.
    /// </summary>
    /// <returns>The updated configuration DTO.</returns>
    public abstract TConfiguration ToConfiguration();

    /// <summary>
    /// Converts this UI model to a page model for rendering.
    /// </summary>
    /// <param name="mode">The page mode (View, Create, Edit).</param>
    /// <returns>A page model containing all sections and components.</returns>
    public abstract IPageModel ToPageModel(IPageMode mode);

    /// <summary>
    /// Validates all component values.
    /// </summary>
    /// <returns>A combined validation result.</returns>
    public abstract ValidationResult Validate();

    /// <summary>
    /// Resets all components to their default values.
    /// </summary>
    public abstract void Reset();

    /// <summary>
    /// Marks the form as having unsaved changes.
    /// </summary>
    protected void MarkAsChanged()
    {
        HasChanges = true;
    }
}
