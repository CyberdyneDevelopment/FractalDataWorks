using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>Builds the coded refusals of a calculation set, each naming the output it happened in and why.</summary>
public static class SetFailure
{
    /// <summary>Builds a refusal with no value.</summary>
    /// <param name="code">The name of a calculation tree result code.</param>
    /// <param name="output">The output (or set) the refusal happened in.</param>
    /// <param name="reason">What is wrong, in a sentence.</param>
    /// <returns>The failed result.</returns>
    public static IGenericResult Of(string code, string output, string reason) =>
        GenericResult.Failure(CalculationTreeResultCodes.ByName(code), ResultDetails.Create("output", output, "reason", reason));

    /// <summary>Builds a refusal that stands for a value.</summary>
    /// <typeparam name="T">The value type the result would have carried.</typeparam>
    /// <param name="code">The name of a calculation tree result code.</param>
    /// <param name="output">The output (or set) the refusal happened in.</param>
    /// <param name="reason">What is wrong, in a sentence.</param>
    /// <returns>The failed result.</returns>
    public static IGenericResult<T> Of<T>(string code, string output, string reason) =>
        GenericResult<T>.Failure(CalculationTreeResultCodes.ByName(code), ResultDetails.Create("output", output, "reason", reason));
}
