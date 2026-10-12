using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// No columns are available to project — neither a projection expression, schema fields,
/// nor container field names were supplied. Prevents a <c>SELECT *</c> from being emitted.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "NoFieldsToProject", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoFieldsToProjectCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoFieldsToProjectCode"/> class.
    /// </summary>
    public NoFieldsToProjectCode()
        : base(21006, "NoFieldsToProject",
            ResultSeverities.ByName("Error"),
            "Cannot build SELECT: no columns available from projection, schema, or container field names",
            isRetryable: false)
    {
    }
}
