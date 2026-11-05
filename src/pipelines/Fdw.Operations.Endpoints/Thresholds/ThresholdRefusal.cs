using Fdw.Operations.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Builds the answer to a threshold request that is refused (FDW-3948).</summary>
public static class ThresholdRefusal
{
    /// <summary>A refusal that names the threshold and says why.</summary>
    /// <typeparam name="TValue">The type the request answers.</typeparam>
    /// <param name="threshold">The threshold's name.</param>
    /// <param name="reason">What is wrong with the request, naming the member.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult<TValue> Of<TValue>(string threshold, string reason) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("ThresholdInvalid"),
            ResultDetails.Create().With("Threshold", threshold).With("Reason", reason)).ToNewResult<TValue>();
}
