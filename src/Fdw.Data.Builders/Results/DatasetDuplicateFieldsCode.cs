using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Dataset has duplicate field names.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "DatasetDuplicateFields", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DatasetDuplicateFieldsCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatasetDuplicateFieldsCode"/> class.
    /// </summary>
    public DatasetDuplicateFieldsCode()
        : base(20001, "DatasetDuplicateFields",
            ResultSeverities.ByName("Error"),
            "Dataset '{DatasetName}' has duplicate field names: {DuplicateFields}",
            isRetryable: false)
    {
    }
}