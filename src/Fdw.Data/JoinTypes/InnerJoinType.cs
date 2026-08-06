using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Inner join - returns only matching records from both sources.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JoinTypes), "Inner", RestrictToCurrentCompilation = true)]
public sealed class InnerJoinType : JoinTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InnerJoinType"/> class.
    /// </summary>
    public InnerJoinType()
        : base(
            id: 1,
            name: "Inner",
            sqlKeyword: "INNER JOIN",
            description: "Returns only matching rows from both sources",
            requiresConditions: true,
            includesAllLeft: false,
            includesAllRight: false)
    {
    }
}
