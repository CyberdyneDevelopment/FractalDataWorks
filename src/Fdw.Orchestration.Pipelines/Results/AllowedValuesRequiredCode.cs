using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Results;

/// <summary>
/// InList validation requires 'AllowedValues' parameter.
/// </summary>
[TypeOption(typeof(PipelineResultCodes), "AllowedValuesRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AllowedValuesRequiredCode : PipelineResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AllowedValuesRequiredCode"/> class.
    /// </summary>
    public AllowedValuesRequiredCode()
        : base(21001, "AllowedValuesRequired",
            ResultSeverities.ByName("Error"),
            "InList validation requires 'AllowedValues' parameter",
            isRetryable: false)
    {
    }
}
