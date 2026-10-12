using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// DELETE was attempted without a WHERE filter.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "MissingDeleteFilter", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingDeleteFilterCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingDeleteFilterCode"/> class.
    /// </summary>
    public MissingDeleteFilterCode()
        : base(21005, "MissingDeleteFilter",
            ResultSeverities.ByName("Error"),
            "DELETE requires a filter — provide a Filter expression to prevent accidental full-table deletion",
            isRetryable: false)
    {
    }
}
