using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// A member listing names a symbol kind that no member of a type has, such as Local or Namespace (FDW-5194).
/// </summary>
/// <remarks>The request is refused rather than read with a value it did not state.</remarks>
[TypeOption(typeof(RoslynResultCodes), "MemberKindNotAMember", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MemberKindNotAMemberCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberKindNotAMemberCode"/> class.
    /// </summary>
    public MemberKindNotAMemberCode()
        : base(21030, "MemberKindNotAMember",
            ResultSeverities.ByName("Error"),
            "'{MemberKind}' is a symbol kind that no member of a type has; use one of: {Accepted}",
            isRetryable: false)
    {
    }
}
