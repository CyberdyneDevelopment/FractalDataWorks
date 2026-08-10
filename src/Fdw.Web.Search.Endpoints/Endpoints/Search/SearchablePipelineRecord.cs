using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Web.Search.Endpoints;

/// <summary>
/// Internal search record for pipelines.
/// </summary>
public class SearchablePipelineRecord
{
    /// <summary>Gets or sets the pipeline identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline type (maps to pipe.Pipeline.ServiceOptionType).</summary>
    [Column("ServiceOptionType")]
    public string PipelineType { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline description.</summary>
    public string? Description { get; set; }
}