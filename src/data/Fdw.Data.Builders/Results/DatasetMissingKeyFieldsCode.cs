using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Dataset must have at least one key field.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "DatasetMissingKeyFields", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DatasetMissingKeyFieldsCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatasetMissingKeyFieldsCode"/> class.
    /// </summary>
    public DatasetMissingKeyFieldsCode()
        : base(21008, "DatasetMissingKeyFields",
            ResultSeverities.ByName("Error"),
            "Dataset '{DatasetName}' must have at least one key field",
            isRetryable: false)
    {
    }
}