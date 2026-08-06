using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// DataStore cannot be null.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "DataStoreNull", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataStoreNullCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreNullCode"/> class.
    /// </summary>
    public DataStoreNullCode()
        : base(21001, "DataStoreNull",
            ResultSeverities.ByName("Error"),
            "DataStore cannot be null",
            isRetryable: false)
    {
    }
}