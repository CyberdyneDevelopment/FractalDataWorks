using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Left join - returns all records from left source, matching records from right source.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JoinTypes), "Left", RestrictToCurrentCompilation = true)]
public sealed class LeftJoinType : JoinTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeftJoinType"/> class.
    /// </summary>
    public LeftJoinType()
        : base(
            id: 2,
            name: "Left",
            sqlKeyword: "LEFT JOIN",
            description: "Returns all rows from left source, matching rows from right source",
            requiresConditions: true,
            includesAllLeft: true,
            includesAllRight: false)
    {
    }
}
