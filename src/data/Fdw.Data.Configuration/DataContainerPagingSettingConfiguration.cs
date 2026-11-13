using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections;

/// <summary>
/// One paging setting an HTTP container states, overriding its connection's (FDW-5562).
/// Generates the table <c>data.DataContainerPagingSetting</c> as a child of <c>data.DataContainer</c>.
/// </summary>
/// <remarks>
/// The name is a key the container's effective paging style reads (<c>PagingPageSize</c>, <c>PagingMaxPages</c>, <c>PagingNextLinkPath</c> ...), and the value
/// is what the container states for it. A key the container leaves out has no row: it takes the connection's value, or the style's declared default. Which
/// names are valid for a style is the style's to say and is checked when the container is saved, so a new style needs no schema change. The same shape as the
/// connection's additional properties (<c>conn.HttpConnectionProperty</c>).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataStore",
    ServiceType = "DataContainerPagingSetting")]
public partial class DataContainerPagingSettingConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this setting.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the setting, a key of the container's paging style. Maps to <c>data.DataContainerPagingSetting.Name</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value the container states for the setting. Maps to <c>data.DataContainerPagingSetting.Value</c>.</summary>
    public string Value { get; set; } = string.Empty;

    // Audit columns

    /// <summary>Whether this is the current active version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
