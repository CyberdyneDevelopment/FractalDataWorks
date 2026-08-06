using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// ExistingDataStoreId cannot be empty.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "ExistingDataStoreIdEmpty", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExistingDataStoreIdEmptyCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExistingDataStoreIdEmptyCode"/> class.
    /// </summary>
    public ExistingDataStoreIdEmptyCode()
        : base(21002, "ExistingDataStoreIdEmpty",
            ResultSeverities.ByName("Error"),
            "ExistingDataStoreId cannot be empty",
            isRetryable: false)
    {
    }
}