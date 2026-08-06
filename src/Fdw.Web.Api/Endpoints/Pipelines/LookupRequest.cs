using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request body for the Lookup-transform parameters on a create/update pipeline transform. Maps onto
/// one <c>PipelineTransformLookupConfiguration</c> cascade-child row per <see cref="LookupColumns"/>
/// entry via <see cref="Fdw.Services.Etl.Transforms.LookupTransformType.MapSpecToConfiguration"/>.
/// </summary>
public class LookupRequest : ILookupSpec
{
    /// <summary>Gets or sets the lookup connection name.</summary>
    [Required]
    public string LookupConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the lookup data set name.</summary>
    [Required]
    public string LookupDataSet { get; set; } = string.Empty;

    /// <summary>Gets or sets the lookup key field (in the lookup source).</summary>
    [Required]
    public string LookupKeyField { get; set; } = string.Empty;

    /// <summary>Gets or sets the source key field to match against.</summary>
    [Required]
    public string SourceKeyField { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional output field prefix.</summary>
    public string? OutputFieldPrefix { get; set; }

    /// <summary>Gets or sets the lookup columns to bring across — one output field per column.</summary>
    [Required]
    public IReadOnlyList<string> LookupColumns { get; set; } = [];

    /// <summary>Gets or sets the join type name, resolved against <c>LookupJoinTypes</c>.</summary>
    [Required]
    public string JoinType { get; set; } = string.Empty;
}
