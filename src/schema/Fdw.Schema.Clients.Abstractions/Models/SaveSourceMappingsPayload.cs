using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Request for saving field mappings for a DataSet source.
/// </summary>
public sealed class SaveSourceMappingsPayload
{
    /// <summary>Gets or sets the DataSet name.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the source name.</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the field mappings to persist.</summary>
    [Required]
    public IList<FieldMappingInputPayload> Mappings { get; set; } = [];
}
