using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// DELETE command requires a filter but none was provided.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "MissingDeleteFilter", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingDeleteFilterCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingDeleteFilterCode"/> class.
    /// </summary>
    public MissingDeleteFilterCode()
        : base(21001, "MissingDeleteFilter",
            ResultSeverities.ByName("Error"),
            "DELETE requires a WHERE clause filter for safety",
            isRetryable: false)
    {
    }
}
