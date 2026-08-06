using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// SQL permission denied (error 229). The database user lacks required schema/table permissions.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "PermissionDenied", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PermissionDeniedCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionDeniedCode"/> class.
    /// </summary>
    public PermissionDeniedCode()
        // Why: Forbidden(403) band (100000-109999) — the caller is authenticated but the DB
        // login lacks the required permission, distinct from Auth(401) not-authenticated failures.
        : base(
            100001,
            "PermissionDenied",
            ResultSeverities.ByName("Error"),
            "Permission denied: the database user lacks access to '{ObjectName}'. Run permissions.sql to grant schema-level access.",
            isRetryable: false)
    {
    }
}
