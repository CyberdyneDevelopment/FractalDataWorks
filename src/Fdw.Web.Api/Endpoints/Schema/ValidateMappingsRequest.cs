using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fdw.Schema.Clients.Models;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Request for validating mappings.
/// </summary>
public class ValidateMappingsRequest
{
    /// <summary>
    /// Gets or sets the DataSet name (from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mappings to validate.
    /// </summary>
    [Required]
    public IList<FieldMappingInputPayload> Mappings { get; set; } = [];
}