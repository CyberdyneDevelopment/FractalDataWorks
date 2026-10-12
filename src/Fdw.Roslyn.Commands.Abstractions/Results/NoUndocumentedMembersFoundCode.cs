using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No undocumented members found.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoUndocumentedMembersFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoUndocumentedMembersFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoUndocumentedMembersFoundCode"/> class.
    /// </summary>
    public NoUndocumentedMembersFoundCode()
        : base(31014, "NoUndocumentedMembersFound",
            ResultSeverities.ByName("Error"),
            "No undocumented members found",
            isRetryable: false)
    {
    }
}
