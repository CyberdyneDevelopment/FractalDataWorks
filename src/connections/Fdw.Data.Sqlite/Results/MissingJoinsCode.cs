using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// CompoundQuery requires at least one JOIN expression.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "MissingJoins", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingJoinsCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingJoinsCode"/> class.
    /// </summary>
    public MissingJoinsCode()
        : base(21006, "MissingJoins",
            ResultSeverities.ByName("Error"),
            "CompoundQuery requires at least one JOIN expression",
            isRetryable: false)
    {
    }
}
