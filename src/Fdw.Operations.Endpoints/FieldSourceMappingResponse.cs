using System;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Mapping from logical field to physical source.
/// </summary>
public class FieldSourceMappingResponse
{
    /// <summary>Gets or sets the source name that provides this field.</summary>
    public string SourceName { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical field name in the source.</summary>
    public string PhysicalField { get; set; } = string.Empty;
}