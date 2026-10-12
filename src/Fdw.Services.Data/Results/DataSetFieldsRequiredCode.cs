using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataSet must have at least one field defined.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataSetFieldsRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetFieldsRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetFieldsRequiredCode"/> class.
    /// </summary>
    public DataSetFieldsRequiredCode()
        : base(21004, "DataSetFieldsRequired", ResultSeverities.ByName("Error"),
            "DataSet must have at least one field defined",
            isRetryable: false)
    {
    }
}