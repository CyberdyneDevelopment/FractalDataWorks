using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Represents no join specified.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JoinTypes), "None", RestrictToCurrentCompilation = true)]
public sealed class NoneJoinType : JoinTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoneJoinType"/> class.
    /// </summary>
    public NoneJoinType()
        : base(
            id: 0,
            name: "None",
            sqlKeyword: "",
            description: "No join specified",
            requiresConditions: false,
            includesAllLeft: false,
            includesAllRight: false)
    {
    }
}
