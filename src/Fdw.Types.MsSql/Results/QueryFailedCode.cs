using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Types.MsSql;

/// <summary>
/// Query failed.
/// </summary>
[TypeOption(typeof(MsSqlTypesResultCodes), "QueryFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class QueryFailedCode : MsSqlTypesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFailedCode"/> class.
    /// </summary>
    public QueryFailedCode()
        : base(71003, "QueryFailed",
            ResultSeverities.ByName("Error"),
            "Failed to query TypeCollection metadata: {ErrorMessage}",
            isRetryable: true)
    {
    }
}