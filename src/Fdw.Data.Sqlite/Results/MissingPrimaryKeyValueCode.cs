using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// An update has no filter and its record does not carry the primary key; there is no WHERE to build, and an UPDATE without one writes every row.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "MissingPrimaryKeyValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingPrimaryKeyValueCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingPrimaryKeyValueCode"/> class.
    /// </summary>
    public MissingPrimaryKeyValueCode()
        : base(21019, "MissingPrimaryKeyValue",
            ResultSeverities.ByName("Error"),
            "The record for container '{Container}' does not carry primary key '{PrimaryKeyField}', and no filter names the rows; UPDATE would write every row",
            isRetryable: false)
    {
    }
}
