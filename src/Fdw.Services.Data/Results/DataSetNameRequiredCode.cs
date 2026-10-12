using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataSet name was null or empty.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataSetNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetNameRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetNameRequiredCode"/> class.
    /// </summary>
    public DataSetNameRequiredCode()
        : base(21005, "DataSetNameRequired", ResultSeverities.ByName("Error"),
            "DataSet name cannot be null or empty",
            isRetryable: false)
    {
    }
}