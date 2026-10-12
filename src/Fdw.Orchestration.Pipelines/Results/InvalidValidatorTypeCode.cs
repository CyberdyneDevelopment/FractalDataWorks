using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Results;

/// <summary>
/// Validator parameter has invalid type.
/// </summary>
[TypeOption(typeof(PipelineResultCodes), "InvalidValidatorType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidValidatorTypeCode : PipelineResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidValidatorTypeCode"/> class.
    /// </summary>
    public InvalidValidatorTypeCode()
        : base(21003, "InvalidValidatorType",
            ResultSeverities.ByName("Error"),
            "Validator must be a Func<IReadOnlyDictionary<string, object?>, ValidationResult> or async equivalent",
            isRetryable: false)
    {
    }
}
