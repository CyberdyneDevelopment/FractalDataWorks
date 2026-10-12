using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Compound query is missing primary container.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "MissingPrimaryContainer", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingPrimaryContainerCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingPrimaryContainerCode"/> class.
    /// </summary>
    public MissingPrimaryContainerCode()
        : base(21003, "MissingPrimaryContainer",
            ResultSeverities.ByName("Error"),
            "Compound query requires a primary container",
            isRetryable: false)
    {
    }
}
