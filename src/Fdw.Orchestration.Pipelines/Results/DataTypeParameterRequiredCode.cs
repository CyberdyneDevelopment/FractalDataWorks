using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Results;

/// <summary>
/// DataType validation requires a 'DataType' parameter.
/// </summary>
[TypeOption(typeof(PipelineResultCodes), "DataTypeParameterRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataTypeParameterRequiredCode : PipelineResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataTypeParameterRequiredCode"/> class.
    /// </summary>
    public DataTypeParameterRequiredCode()
        : base(21000, "DataTypeParameterRequired",
            ResultSeverities.ByName("Error"),
            "DataType validation requires a 'DataType' parameter",
            isRetryable: false)
    {
    }
}
