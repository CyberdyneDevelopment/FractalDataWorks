using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// A member listing names a member kind that is not one of the kinds it filters by (FDW-5194).
/// </summary>
/// <remarks>The request is refused rather than read with a value it did not state.</remarks>
[TypeOption(typeof(RoslynResultCodes), "MemberKindNotKnown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MemberKindNotKnownCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberKindNotKnownCode"/> class.
    /// </summary>
    public MemberKindNotKnownCode()
        : base(21029, "MemberKindNotKnown",
            ResultSeverities.ByName("Error"),
            "'{MemberKind}' is not a member kind; use one of: {Accepted}",
            isRetryable: false)
    {
    }
}
