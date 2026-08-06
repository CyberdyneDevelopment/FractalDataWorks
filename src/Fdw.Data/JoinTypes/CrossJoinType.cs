using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Cross join - returns Cartesian product of both sources (all combinations).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JoinTypes), "Cross", RestrictToCurrentCompilation = true)]
public sealed class CrossJoinType : JoinTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrossJoinType"/> class.
    /// </summary>
    public CrossJoinType()
        : base(
            id: 5,
            name: "Cross",
            sqlKeyword: "CROSS JOIN",
            description: "Returns Cartesian product of both sources (all combinations)",
            requiresConditions: false,
            includesAllLeft: true,
            includesAllRight: true)
    {
    }
}
