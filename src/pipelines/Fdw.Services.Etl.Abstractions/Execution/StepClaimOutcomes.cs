namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>The outcomes a claimed step records (FDW-6139); the column of the claim allows no other text.</summary>
public static class StepClaimOutcomes
{
    /// <summary>The step acted and finished.</summary>
    public const string Succeeded = "Succeeded";

    /// <summary>The step acted and failed; the claim is kept, so it is not tried a second time.</summary>
    public const string Failed = "Failed";
}
