using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fdw.Schema.Clients.Models;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Request for saving mappings for a source.
/// </summary>
public class SaveSourceMappingsRequest
{
    /// <summary>
    /// Gets or sets the DataSet name (from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source name (from route).
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mappings to save.
    /// </summary>
    [Required]
    public IList<FieldMappingInputPayload> Mappings { get; set; } = [];
}