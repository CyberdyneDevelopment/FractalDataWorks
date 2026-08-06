using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Response DTO surfacing the Lookup-transform parameters on a pipeline detail response, read from the
/// composed aggregate's typed <c>Lookups</c> cascade children (one row per brought-across column,
/// collapsed back into a single shared-connection/keys view).
/// </summary>
public class LookupDto
{
    /// <summary>Gets or sets the lookup connection name.</summary>
    public string LookupConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the lookup data set name.</summary>
    public string LookupDataSet { get; set; } = string.Empty;

    /// <summary>Gets or sets the lookup key field (in the lookup source).</summary>
    public string LookupKeyField { get; set; } = string.Empty;

    /// <summary>Gets or sets the source key field matched against.</summary>
    public string SourceKeyField { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional output field prefix.</summary>
    public string? OutputFieldPrefix { get; set; }

    /// <summary>Gets or sets the lookup columns brought across — one output field per column.</summary>
    public IReadOnlyList<string> LookupColumns { get; set; } = [];

    /// <summary>Gets or sets the join type name.</summary>
    public string JoinType { get; set; } = string.Empty;
}
