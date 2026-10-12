using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// Query failed.
/// </summary>
[TypeOption(typeof(UserResultCodes), "QueryFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class QueryFailedCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFailedCode"/> class.
    /// </summary>
    public QueryFailedCode()
        : base(70001, "QueryFailed",
            ResultSeverities.ByName("Error"),
            "Failed to query user data: {ErrorMessage}",
            isRetryable: true)
    {
    }
}