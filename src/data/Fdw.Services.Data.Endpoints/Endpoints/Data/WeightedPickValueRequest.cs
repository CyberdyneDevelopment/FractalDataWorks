namespace Fdw.Services.Data.Endpoints;

/// <summary>One weighted option within a WeightedPick stand-in.</summary>
public class WeightedPickValueRequest
{
    /// <summary>Gets or sets the value this option generates when picked.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets how likely this option is relative to the pick's other options.</summary>
    public int Weight { get; set; }

    /// <summary>Gets or sets this option's display order.</summary>
    public int Ordinal { get; set; }
}
