using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Results;

/// <summary>
/// Custom validation requires a 'Validator' parameter.
/// </summary>
[TypeOption(typeof(PipelineResultCodes), "ValidatorParameterRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ValidatorParameterRequiredCode : PipelineResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorParameterRequiredCode"/> class.
    /// </summary>
    public ValidatorParameterRequiredCode()
        : base(21002, "ValidatorParameterRequired",
            ResultSeverities.ByName("Error"),
            "Custom validation requires a 'Validator' parameter",
            isRetryable: false)
    {
    }
}
