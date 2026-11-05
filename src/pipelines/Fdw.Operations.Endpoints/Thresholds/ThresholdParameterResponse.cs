namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>A parameter a kind of threshold takes (FDW-3948).</summary>
public sealed class ThresholdParameterResponse
{
    /// <summary>Gets or sets the parameter name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the type of its value: Int64, Double, String, Boolean, TimeSpan or DateTimeOffset.</summary>
    public string ValueType { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether a threshold of the type must state it.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets a value indicating whether it is stated as several values in order instead of one.</summary>
    public bool IsList { get; set; }

    /// <summary>Gets or sets what it means.</summary>
    public string Description { get; set; } = string.Empty;
}
