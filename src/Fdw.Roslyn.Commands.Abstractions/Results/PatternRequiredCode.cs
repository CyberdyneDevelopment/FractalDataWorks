using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Pattern is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "PatternRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PatternRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PatternRequiredCode"/> class.
    /// </summary>
    public PatternRequiredCode()
        : base(21009, "PatternRequired",
            ResultSeverities.ByName("Error"),
            "Pattern is required",
            isRetryable: false)
    {
    }
}
