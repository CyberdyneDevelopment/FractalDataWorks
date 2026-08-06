using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Right join - returns all records from right source, matching records from left source.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JoinTypes), "Right", RestrictToCurrentCompilation = true)]
public sealed class RightJoinType : JoinTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RightJoinType"/> class.
    /// </summary>
    public RightJoinType()
        : base(
            id: 3,
            name: "Right",
            sqlKeyword: "RIGHT JOIN",
            description: "Returns all rows from right source, matching rows from left source",
            requiresConditions: true,
            includesAllLeft: false,
            includesAllRight: true)
    {
    }
}
