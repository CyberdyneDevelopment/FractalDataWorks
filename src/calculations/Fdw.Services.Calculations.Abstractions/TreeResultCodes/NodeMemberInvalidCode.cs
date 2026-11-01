using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A node member (name, value, places, mode) is missing, unwanted or out of range.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "NodeMemberInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NodeMemberInvalidCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeMemberInvalidCode"/> class.
    /// </summary>
    public NodeMemberInvalidCode()
        : base(
            22012,
            "NodeMemberInvalid",
            ResultSeverities.ByName("Error"),
            "Calculation tree refused at {node}: a member of the node is missing or invalid. {reason}",
            isRetryable: false)
    {
    }
}
