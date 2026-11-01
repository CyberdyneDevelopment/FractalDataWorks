using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// Command does not implement the required interface for this translator.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "InvalidCommandType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidCommandTypeCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCommandTypeCode"/> class.
    /// </summary>
    public InvalidCommandTypeCode()
        : base(21009, "InvalidCommandType",
            ResultSeverities.ByName("Error"),
            "Command does not implement the required interface for this translator",
            isRetryable: false)
    {
    }
}
