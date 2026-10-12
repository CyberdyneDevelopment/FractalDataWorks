using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Failed to create configuration writers.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "WritersCreationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WritersCreationFailedCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WritersCreationFailedCode"/> class.
    /// </summary>
    public WritersCreationFailedCode()
        : base(91001, "WritersCreationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to create configuration writers",
            isRetryable: false)
    {
    }
}