using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Invalid regex pattern.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "InvalidRegexPattern", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidRegexPatternCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRegexPatternCode"/> class.
    /// </summary>
    public InvalidRegexPatternCode()
        : base(20001, "InvalidRegexPattern",
            ResultSeverities.ByName("Error"),
            "Invalid regex pattern: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
