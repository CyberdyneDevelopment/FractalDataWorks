using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// SQL query timeout (error -2). The query exceeded the configured timeout period.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "QueryTimeout", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class QueryTimeoutCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTimeoutCode"/> class.
    /// </summary>
    public QueryTimeoutCode()
        : base(
            110001,
            "QueryTimeout",
            ResultSeverities.ByName("Warning"),
            "Query timed out on '{CommandText}'. Consider optimizing the query or increasing the timeout.",
            isRetryable: true)
    {
    }
}
