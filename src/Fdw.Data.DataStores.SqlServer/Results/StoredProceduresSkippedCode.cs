using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Stored procedures skipped per options.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "StoredProceduresSkipped", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StoredProceduresSkippedCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoredProceduresSkippedCode"/> class.
    /// </summary>
    public StoredProceduresSkippedCode()
        : base(11000, "StoredProceduresSkipped",
            ResultSeverities.ByName("Warning"),
            "Stored procedures skipped per options",
            isRetryable: false)
    {
    }
}