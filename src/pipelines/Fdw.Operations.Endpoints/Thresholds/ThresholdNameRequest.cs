namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Request naming a threshold (FDW-3948): the route of the endpoints that act on one.</summary>
public sealed class ThresholdNameRequest
{
    /// <summary>Gets or sets the threshold's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;
}
