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
/// Explicit system elevation: sets <c>SESSION_CONTEXT('System') = '1'</c>, and <c>TenantId</c> when
/// the elevation is working for a tenant.
/// </summary>
/// <remarks>
/// <para>
/// System is a positive assertion, never an absence. <c>security.fn_TenantFilter</c> admits every row
/// to a connection carrying <c>System = '1'</c>, and admits NOTHING to one carrying no keys at all.
/// Before FDW-825 the elevation was the absence of <c>UserId</c>, so "this connection applies no
/// session context" and "this connection is fully system-elevated" were the same bytes on the wire —
/// any connection that bypassed this layer read every tenant's rows.
/// </para>
/// <para>
/// The key is written read-only, so nothing that runs on the connection afterwards can unset it or
/// add a user identity to it.
/// </para>
/// <para>
/// <c>TenantId</c> never narrows what a system connection sees. It is set so that rows a system
/// elevation writes — a background run's execution records and outputs — default to the tenant the
/// run belongs to, through every tenant column's <c>DEFAULT</c>, rather than to the default tenant.
/// </para>
/// <para>
/// Named <c>SystemContext</c> rather than <c>System</c>: a TypeCollection member named <c>System</c>
/// would shadow the <c>System</c> namespace inside the generated collection body, which emits
/// unqualified <c>System.Type</c> and <c>System.Threading.Tasks.Task.CompletedTask</c>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlSessionContextTypes), "SystemContext")]
public sealed class SystemSessionContext() : MsSqlSessionContextBase("SystemContext")
{
    /// <inheritdoc />
    public override bool Governs(IAuthenticationContext? authenticationContext)
        => IsSystemElevation(authenticationContext);

    /// <inheritdoc />
    public override SessionContextPlan Plan(IAuthenticationContext? authenticationContext)
        => SessionContextPlan.ForSystem(authenticationContext?.ActiveTenantId);

    /// <inheritdoc />
    /// <remarks>
    /// Unbounded. The system branch of <c>fn_TenantFilter</c> tests the <c>System</c> key alone — it
    /// joins no table and consults no grant, so no edit to authorization data can narrow or widen what
    /// this context sees. There is nothing to go stale.
    /// </remarks>
    public override TimeSpan MaxCacheDuration(IAuthenticationContext? authenticationContext)
        => TimeSpan.MaxValue;

    /// <inheritdoc />
    public override SessionContextStatement Statement(SessionContextPlan plan, string parameterPrefix)
    {
        var sql = "EXEC sp_set_session_context @key = N'System', @value = N'1', @read_only = 1;";
        var parameters = new List<SessionContextParameter>();

        if (plan.TenantId.HasValue)
        {
            var tenantId = string.Concat(parameterPrefix, "tenantId");
            sql += $" EXEC sp_set_session_context @key = N'TenantId', @value = @{tenantId}, @read_only = 1;";
            parameters.Add(new SessionContextParameter(tenantId, SqlDbType.UniqueIdentifier, plan.TenantId.Value));
        }

        return new SessionContextStatement(sql, parameters);
    }

    /// <inheritdoc />
    public override void LogApplied(SessionContextPlan plan, SqlConnection connection, ILogger logger)
    {
        if (plan.TenantId.HasValue)
        {
            MsSqlConnectionLogger.TraceSystemContextSetForTenant(logger, plan.TenantId.Value.ToString());
        }
        else
        {
            MsSqlConnectionLogger.TraceSystemContextSet(logger);
        }
    }
}
