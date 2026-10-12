using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// Container path is not an IDatabasePath.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "InvalidContainerPath", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidContainerPathCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidContainerPathCode"/> class.
    /// </summary>
    public InvalidContainerPathCode()
        : base(21001, "InvalidContainerPath",
            ResultSeverities.ByName("Error"),
            "Container path is not an IDatabasePath — ensure a SqliteDatabasePath is used",
            isRetryable: false)
    {
    }
}
