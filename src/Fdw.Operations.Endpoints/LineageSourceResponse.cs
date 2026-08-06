using System;
using System.Reflection;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Upstream source in lineage.
/// </summary>
public class LineageSourceResponse
{
    /// <summary>Gets or sets the source name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the source type (e.g., Database, REST API, File).</summary>
    public string SourceType { get; set; } = string.Empty;
    /// <summary>Gets or sets the connection name used by this source.</summary>
    public string? ConnectionName { get; set; }
    /// <summary>Gets or sets the data store name used by this source.</summary>
    public string? DataStoreName { get; set; }
    /// <summary>Gets or sets the physical location (e.g., schema.table, URL, file path).</summary>
    public string? PhysicalLocation { get; set; }
    /// <summary>Gets or sets the source priority for the DataSet.</summary>
    public int Priority { get; set; }
}