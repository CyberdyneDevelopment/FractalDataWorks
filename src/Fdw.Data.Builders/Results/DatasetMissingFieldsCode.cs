using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Dataset must have at least one field.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "DatasetMissingFields", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DatasetMissingFieldsCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatasetMissingFieldsCode"/> class.
    /// </summary>
    public DatasetMissingFieldsCode()
        : base(21007, "DatasetMissingFields",
            ResultSeverities.ByName("Error"),
            "Dataset '{DatasetName}' must have at least one field",
            isRetryable: false)
    {
    }
}