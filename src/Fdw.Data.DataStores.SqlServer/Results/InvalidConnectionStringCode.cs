using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Invalid connection string.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "InvalidConnectionString", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidConnectionStringCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConnectionStringCode"/> class.
    /// </summary>
    public InvalidConnectionStringCode()
        : base(20001, "InvalidConnectionString",
            ResultSeverities.ByName("Error"),
            "Invalid connection string: {error}",
            isRetryable: false)
    {
    }
}