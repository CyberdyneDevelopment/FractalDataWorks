using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// Missing or invalid DataTable for bulk copy.
/// </summary>
[TypeOption(typeof(MsSqlConnectionResultCodes), "InvalidBulkCopyDataTable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidBulkCopyDataTableCode : MsSqlConnectionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidBulkCopyDataTableCode"/> class.
    /// </summary>
    public InvalidBulkCopyDataTableCode()
        : base(
            20001,
            "InvalidBulkCopyDataTable",
            ResultSeverities.ByName("Error"),
            "Missing or invalid DataTable for bulk copy",
            isRetryable: false)
    {
    }
}
