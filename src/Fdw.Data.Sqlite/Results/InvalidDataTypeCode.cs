using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// Command data is not an IEnumerable.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "InvalidDataType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidDataTypeCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidDataTypeCode"/> class.
    /// </summary>
    public InvalidDataTypeCode()
        : base(21008, "InvalidDataType",
            ResultSeverities.ByName("Error"),
            "Command data is not an IEnumerable — use an InsertCommand with a collection for BatchInsert",
            isRetryable: false)
    {
    }
}
