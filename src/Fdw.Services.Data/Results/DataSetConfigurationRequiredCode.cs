using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataSet configuration was null.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataSetConfigurationRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetConfigurationRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetConfigurationRequiredCode"/> class.
    /// </summary>
    public DataSetConfigurationRequiredCode()
        : base(21003, "DataSetConfigurationRequired", ResultSeverities.ByName("Error"),
            "DataSet configuration cannot be null",
            isRetryable: false)
    {
    }
}