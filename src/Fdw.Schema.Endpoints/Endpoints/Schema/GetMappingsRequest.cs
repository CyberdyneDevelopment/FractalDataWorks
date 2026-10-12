using System;
using System.Collections.Generic;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Request for getting mappings by DataSet name.
/// </summary>
public class GetMappingsRequest
{
    /// <summary>
    /// Gets or sets the DataSet name (from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;
}