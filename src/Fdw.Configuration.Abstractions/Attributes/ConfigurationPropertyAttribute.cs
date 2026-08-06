using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Configuration;

/// <summary>
/// Provides additional metadata for configuration properties.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to customize how a property is rendered in UI and persisted in the database.
/// Properties without this attribute use smart defaults based on their type and name.
/// </para>
/// <para>
/// Smart defaults:
/// </para>
/// <list type="bullet">
/// <item><description>Label: Property name with spaces (e.g., "ConnectionTimeout" becomes "Connection Timeout")</description></item>
/// <item><description>String MaxLength: 255</description></item>
/// <item><description>*TypeId properties: Auto-detected as TypeOption references, rendered as dropdowns</description></item>
/// <item><description>Collections: Rendered as sub-tables with Ordinal column for ordering</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public sealed class ConnectionConfiguration : ConfigurationBase&lt;ConnectionConfiguration&gt;
/// {
///     [ConfigurationProperty(Label = "Connection Name", HelpText = "A unique name for this connection")]
///     [Required]
///     public string Name { get; set; } = "";
///
///     [ConfigurationProperty(Label = "Port Number", Order = 3)]
///     [Range(1, 65535)]
///     public int Port { get; set; } = 1433;
///
///     [ConfigurationProperty(IsHidden = true)]
///     public string? InternalId { get; set; }
/// }
/// </code>
/// </example>
// Why: pure attribute definition (declarative metadata only, consumed by UI/DDL generators) — no logic to unit test.
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the display label for this property.
    /// </summary>
    /// <remarks>
    /// If not specified, generated from property name with spaces.
    /// </remarks>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the help text displayed to users.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text for input fields.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the display order of this property in the UI.
    /// </summary>
    /// <remarks>
    /// Properties are sorted by Order ascending, then by declaration order.
    /// Default is int.MaxValue to preserve declaration order when not specified.
    /// </remarks>
    public int Order { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets a value indicating whether this property is hidden from the UI.
    /// </summary>
    /// <remarks>
    /// Hidden properties are still persisted to the database but not shown in forms.
    /// </remarks>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this property is read-only in the UI.
    /// </summary>
    /// <remarks>
    /// Read-only properties are displayed but cannot be edited.
    /// </remarks>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the maximum length for string properties.
    /// </summary>
    /// <remarks>
    /// Defaults to 255 if not specified. Used for both UI validation and DDL generation.
    /// </remarks>
    public int MaxLength { get; set; } = 255;

    /// <summary>
    /// Gets or sets the database column name.
    /// </summary>
    /// <remarks>
    /// If not specified, uses the property name.
    /// </remarks>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets the section this property belongs to in the UI.
    /// </summary>
    /// <remarks>
    /// Properties with the same section are grouped together.
    /// Default section is "General".
    /// </remarks>
    public string Section { get; set; } = "General";

    /// <summary>
    /// Gets or sets the column width (1-12 grid system) for this property in the UI.
    /// </summary>
    /// <remarks>
    /// Defaults to 6 (half width). Use 12 for full width.
    /// </remarks>
    public int Width { get; set; } = 6;
}
