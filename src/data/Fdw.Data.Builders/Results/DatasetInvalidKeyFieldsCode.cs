using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Dataset has key fields that don't exist in the field list.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "DatasetInvalidKeyFields", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DatasetInvalidKeyFieldsCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatasetInvalidKeyFieldsCode"/> class.
    /// </summary>
    public DatasetInvalidKeyFieldsCode()
        : base(21009, "DatasetInvalidKeyFields",
            ResultSeverities.ByName("Error"),
            "Dataset '{DatasetName}' has key fields that don't exist in the field list: {InvalidKeyFields}",
            isRetryable: false)
    {
    }
}