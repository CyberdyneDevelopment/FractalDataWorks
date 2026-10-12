using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Results;

/// <summary>
/// Regex validation requires a 'Pattern' parameter.
/// </summary>
[TypeOption(typeof(PipelineResultCodes), "RegexPatternRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RegexPatternRequiredCode : PipelineResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegexPatternRequiredCode"/> class.
    /// </summary>
    public RegexPatternRequiredCode()
        : base(20000, "RegexPatternRequired",
            ResultSeverities.ByName("Error"),
            "Regex validation requires a 'Pattern' parameter",
            isRetryable: false)
    {
    }
}
