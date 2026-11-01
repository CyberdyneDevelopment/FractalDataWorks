using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Results;

/// <summary>
/// Invalid regex pattern provided.
/// </summary>
[TypeOption(typeof(PipelineResultCodes), "InvalidRegexPattern", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidRegexPatternCode : PipelineResultCodeBase
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
