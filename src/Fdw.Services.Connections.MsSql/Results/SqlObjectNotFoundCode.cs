using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// SQL object not found (error 208). The referenced table or view does not exist.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "SqlObjectNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlObjectNotFoundCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlObjectNotFoundCode"/> class.
    /// </summary>
    public SqlObjectNotFoundCode()
        : base(
            30000,
            "SqlObjectNotFound",
            ResultSeverities.ByName("Error"),
            "Table or view not found: '{ObjectName}'. The DDL may not have been deployed to the database.",
            isRetryable: false)
    {
    }
}
