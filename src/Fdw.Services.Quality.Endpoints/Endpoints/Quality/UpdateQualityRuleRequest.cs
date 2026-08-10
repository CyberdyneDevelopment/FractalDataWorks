using System;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Request containing the properties needed to update an existing quality rule.</summary>
public class UpdateQualityRuleRequest
{
    /// <summary>Gets or sets the unique identifier of the quality rule (bound from the route).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the rule name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the rule description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the rule expression.</summary>
    public string? Expression { get; set; }

    /// <summary>Gets or sets whether the rule is enabled.</summary>
    public bool IsEnabled { get; set; }
}
