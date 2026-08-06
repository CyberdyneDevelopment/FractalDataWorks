using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Full outer join - returns all records from both sources.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JoinTypes), "Full", RestrictToCurrentCompilation = true)]
public sealed class FullJoinType : JoinTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FullJoinType"/> class.
    /// </summary>
    public FullJoinType()
        : base(
            id: 4,
            name: "Full",
            sqlKeyword: "FULL OUTER JOIN",
            description: "Returns all rows from both sources",
            requiresConditions: true,
            includesAllLeft: true,
            includesAllRight: true)
    {
    }
}
