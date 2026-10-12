using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Container path is not a PostgreSQL database path.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "InvalidContainerPath", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidContainerPathCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidContainerPathCode"/> class.
    /// </summary>
    public InvalidContainerPathCode()
        : base(21005, "InvalidContainerPath",
            ResultSeverities.ByName("Error"),
            "Container path must be a PostgreSqlDatabasePath",
            isRetryable: false)
    {
    }
}
