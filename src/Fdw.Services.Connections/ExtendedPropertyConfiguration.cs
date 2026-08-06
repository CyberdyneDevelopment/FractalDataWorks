using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Connections;

/// <summary>
/// Configuration class for extended properties captured from database objects.
/// Generates the table <c>cfg.ExtendedProperty</c>.
/// </summary>
/// <remarks>
/// <para>
/// Extended properties are metadata attached to database objects in SQL Server.
/// Common properties include:
/// <list type="bullet">
/// <item><description><c>MS_Description</c> - Human-readable description of the object</description></item>
/// <item><description><c>MS_Label</c> - Display label for the object</description></item>
/// <item><description>Custom properties defined by developers</description></item>
/// </list>
/// </para>
/// <para>
/// This class captures extended properties discovered during schema import and stores
/// them in a normalized table for later retrieval and display.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Obsolete("Extended properties should be stored as Description on DataContainer/DataContainerField, not in a separate table.")]
public partial class ExtendedPropertyConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this extended property record.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for configuration binding (formatted as {PropertyName}@{TargetType}:{TargetId}).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the section name for configuration binding.
    /// </summary>
    public string SectionName => "ExtendedProperties";

    /// <summary>
    /// Gets the service type - always "DataStore" for extended properties.
    /// </summary>
    public string ServiceType => "DataStore";

    /// <summary>
    /// Gets the service option type - null for extended properties.
    /// </summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the ID of the target object (DataStore, DataPath, DataContainer, or DataContainerField).
    /// </summary>
    public Guid TargetId { get; set; }

    /// <summary>
    /// Gets or sets the type of target object.
    /// </summary>
    /// <value>One of: "DataStore", "DataPath", "DataContainer", "DataContainerField"</value>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the extended property (e.g., "MS_Description").
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the extended property.
    /// </summary>
    public string? PropertyValue { get; set; }

    /// <summary>
    /// Gets or sets the Level 0 type from SQL Server extended properties (e.g., "SCHEMA").
    /// </summary>
    public string? Level0Type { get; set; }

    /// <summary>
    /// Gets or sets the Level 0 name from SQL Server extended properties (e.g., "dbo").
    /// </summary>
    public string? Level0Name { get; set; }

    /// <summary>
    /// Gets or sets the Level 1 type from SQL Server extended properties (e.g., "TABLE").
    /// </summary>
    public string? Level1Type { get; set; }

    /// <summary>
    /// Gets or sets the Level 1 name from SQL Server extended properties (e.g., "Customers").
    /// </summary>
    public string? Level1Name { get; set; }

    /// <summary>
    /// Gets or sets the Level 2 type from SQL Server extended properties (e.g., "COLUMN").
    /// </summary>
    public string? Level2Type { get; set; }

    /// <summary>
    /// Gets or sets the Level 2 name from SQL Server extended properties (e.g., "Email").
    /// </summary>
    public string? Level2Name { get; set; }

}
