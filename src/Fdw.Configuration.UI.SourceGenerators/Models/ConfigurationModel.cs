using System.Collections.Generic;

namespace Fdw.Configuration.UI.SourceGenerators.Models;

/// <summary>
/// Analyzed model of a configuration class.
/// </summary>
public sealed class ConfigurationModel
{
    /// <summary>
    /// Gets or sets the namespace of the configuration class.
    /// </summary>
    public string Namespace { get; set; } = "";

    /// <summary>
    /// Gets or sets the class name.
    /// </summary>
    public string ClassName { get; set; } = "";

    /// <summary>
    /// Gets or sets the properties of the configuration class.
    /// </summary>
    public IList<PropertyModel> Properties { get; set; } = new List<PropertyModel>();

    /// <summary>
    /// Gets or sets whether the configuration has nested collections.
    /// </summary>
    public bool HasNestedCollections { get; set; }

    /// <summary>
    /// Gets or sets whether to generate Web components. Defaults to true.
    /// </summary>
    public bool GenerateWeb { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate Blazor components. Defaults to true.
    /// </summary>
    public bool GenerateBlazor { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate TUI components. Defaults to true.
    /// </summary>
    public bool GenerateTui { get; set; } = true;

    /// <summary>
    /// Gets or sets the display name for the configuration.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description for the configuration.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the service category for the configuration.
    /// </summary>
    public string? ServiceCategory { get; set; }

    /// <summary>
    /// Gets or sets the service type for the configuration.
    /// </summary>
    public string? ServiceType { get; set; }
}
