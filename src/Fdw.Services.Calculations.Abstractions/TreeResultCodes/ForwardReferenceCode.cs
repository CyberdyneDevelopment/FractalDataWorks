using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A Ref node names a result that is later in the list, is its own result, or does not exist.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "ForwardReference", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ForwardReferenceCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForwardReferenceCode"/> class.
    /// </summary>
    public ForwardReferenceCode()
        : base(
            22014,
            "ForwardReference",
            ResultSeverities.ByName("Error"),
            "Calculation tree refused at {node}: a reference names a result that is not defined earlier. {reason}",
            isRetryable: false)
    {
    }
}
