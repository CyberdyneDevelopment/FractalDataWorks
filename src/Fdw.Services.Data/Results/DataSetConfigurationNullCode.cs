using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataSet configuration was null for predicate pushdown analysis.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataSetConfigurationNull", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetConfigurationNullCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetConfigurationNullCode"/> class.
    /// </summary>
    public DataSetConfigurationNullCode()
        : base(21002, "DataSetConfigurationNull", ResultSeverities.ByName("Error"),
            "DataSet configuration cannot be null",
            isRetryable: false)
    {
    }
}