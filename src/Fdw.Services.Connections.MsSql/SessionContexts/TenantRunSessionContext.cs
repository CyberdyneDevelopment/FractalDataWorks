using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// A tenant run -- a tenant's work, such as a pipeline, acting as no person: sets <c>TenantRun</c>,
/// <c>UserId</c> (the run's principal) and <c>TenantId</c>, which <c>security.fn_TenantFilter</c>'s
/// tenant-run branch reads (FDW-1087).
/// </summary>
/// <remarks>
/// <para>
/// A tenant run sees its own tenant's rows and shared rows, never the system tenant's, a Restricted or
/// an Org-narrowed row; it writes only its own tenant's rows.
/// </para>
/// <para>
/// It is not a user: no membership names it, so the user branches never admit it, and it is not a
/// system elevation, so nothing that refuses a non-system caller lets it through.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlSessionContextTypes), "TenantRun")]
public sealed class TenantRunSessionContext() : MsSqlSessionContextBase("TenantRun")
{
    /// <inheritdoc />
    public override bool Governs(IAuthenticationContext? authenticationContext)
        => IsTenantRun(authenticationContext);

    /// <inheritdoc />
    public override SessionContextPlan Plan(IAuthenticationContext? authenticationContext)
    {
        if (!IsTenantRun(authenticationContext))
        {
            throw new InvalidOperationException(
                $"{nameof(TenantRunSessionContext)}.{nameof(Plan)} was called with an authentication context it does not govern.");
        }

        var run = (TenantRunAuthenticationContext)authenticationContext!;
        return SessionContextPlan.ForTenantRun(run.PrincipalId, run.ActiveTenantId!.Value);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Unbounded. The tenant-run branch tests the row's own <c>TenantId</c> and <c>VisibilityGroupId</c>
    /// against the session's keys and joins no grant table, so nothing out of band can change what a
    /// given run principal in a given tenant sees.
    /// </remarks>
    public override TimeSpan MaxCacheDuration(IAuthenticationContext? authenticationContext)
        => TimeSpan.MaxValue;

    /// <inheritdoc />
    public override SessionContextStatement Statement(SessionContextPlan plan, string parameterPrefix)
    {
        var userId = string.Concat(parameterPrefix, "userId");
        var tenantId = string.Concat(parameterPrefix, "tenantId");
        var sql = $"EXEC sp_set_session_context @key = N'UserId', @value = @{userId}, @read_only = 1;"
            + $" EXEC sp_set_session_context @key = N'TenantId', @value = @{tenantId}, @read_only = 1;"
            + " EXEC sp_set_session_context @key = N'TenantRun', @value = N'1', @read_only = 1;";
        return new SessionContextStatement(sql, new List<SessionContextParameter>
        {
            new(userId, SqlDbType.UniqueIdentifier, plan.UserId!.Value),
            new(tenantId, SqlDbType.UniqueIdentifier, plan.TenantId!.Value),
        });
    }

    /// <inheritdoc />
    public override void LogApplied(SessionContextPlan plan, SqlConnection connection, ILogger logger)
        => MsSqlConnectionLogger.TraceTenantContextSet(logger, plan.TenantId!.Value.ToString());
}
