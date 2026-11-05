namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>A threshold as a list shows it (FDW-3948).</summary>
public sealed class ThresholdSummaryResponse
{
    /// <summary>Gets or sets the identifier.</summary>
    public System.Guid Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the threshold is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the threshold type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of subject the threshold judges.</summary>
    public string SubjectKind { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the threshold is judged.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether a rule fires only when its criteria start to hold.</summary>
    public bool FiresOnEntry { get; set; }

    /// <summary>Gets or sets how many rules the threshold has.</summary>
    public int RuleCount { get; set; }
}
