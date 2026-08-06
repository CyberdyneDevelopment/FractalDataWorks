using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Compound query is missing joined containers.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "MissingJoinedContainers", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingJoinedContainersCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingJoinedContainersCode"/> class.
    /// </summary>
    public MissingJoinedContainersCode()
        : base(21002, "MissingJoinedContainers",
            ResultSeverities.ByName("Error"),
            "Compound query requires at least one joined container",
            isRetryable: false)
    {
    }
}
