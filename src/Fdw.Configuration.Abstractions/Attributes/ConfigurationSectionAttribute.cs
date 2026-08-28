using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Configuration;

/// <summary>
/// Defines a section in the configuration UI form.
/// </summary>
/// <remarks>
/// <para>
/// Sections group related properties together in the generated UI.
/// Apply this attribute at the class level to define available sections,
/// then reference section names in <see cref="ConfigurationPropertyAttribute.Section"/>.
/// </para>
/// <para>
/// Multiple <see cref="ConfigurationSectionAttribute"/> attributes can be applied to define multiple sections.
/// Sections are rendered in order of their <see cref="Order"/> property.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [ConfigurationSection("Basic", Title = "Basic Settings", Order = 1)]
/// [ConfigurationSection("Authentication", Title = "Authentication", Order = 2)]
/// [ConfigurationSection("Advanced", Title = "Advanced Settings", Order = 3, IsCollapsible = true, IsExpanded = false)]
/// public sealed class ConnectionConfiguration : ConfigurationBase&lt;ConnectionConfiguration&gt;
/// {
///     [ConfigurationProperty(Section = "Basic")]
///     public string Name { get; set; } = "";
///
///     [ConfigurationProperty(Section = "Authentication")]
///     public int AuthenticationTypeId { get; set; }
///
///     [ConfigurationProperty(Section = "Advanced")]
///     public int TimeoutSeconds { get; set; } = 30;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationSectionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationSectionAttribute"/> class.
    /// </summary>
    /// <param name="name">The unique identifier for this section.</param>
    public ConfigurationSectionAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Gets the unique identifier for this section.
    /// </summary>
    /// <remarks>
    /// This is the value used in <see cref="ConfigurationPropertyAttribute.Section"/>.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the display title for this section.
    /// </summary>
    /// <remarks>
    /// If not specified, uses <see cref="Name"/> with spaces added.
    /// </remarks>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description displayed below the section title.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the display order of this section.
    /// </summary>
    /// <remarks>
    /// Sections are sorted by Order ascending.
    /// Default is int.MaxValue to preserve declaration order when not specified.
    /// </remarks>
    public int Order { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets a value indicating whether this section can be collapsed.
    /// </summary>
    public bool IsCollapsible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this section is initially expanded.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="IsCollapsible"/> is true.
    /// Defaults to true (expanded).
    /// </remarks>
    public bool IsExpanded { get; set; } = true;
}
