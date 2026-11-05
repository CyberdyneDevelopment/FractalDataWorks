namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>The domain every threshold belongs to (FDW-3948): the name the domain provider writes a threshold under.</summary>
public static class ThresholdDomain
{
    /// <summary>The domain name; it is the container the threshold provider is registered over.</summary>
    public const string Name = "Threshold";
}
