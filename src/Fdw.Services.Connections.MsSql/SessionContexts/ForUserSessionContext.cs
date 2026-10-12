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
/// A real, authenticated, <see cref="Guid"/>-identified user: sets the
/// <c>UserId</c> / <c>TenantId</c> key set that
/// <c>security.fn_TenantFilter</c> reads.
/// </summary>
/// <remarks>
/// <para>
/// <c>TenantId</c> is the tenant the caller is acting in — the tenant their token was issued for.
/// It decides everything the connection sees: acting in the global tenant sees every tenant's rows,
/// acting in any other tenant sees that tenant's rows plus shared ones, and either way only while
/// the user holds a membership the predicate can find. There is no cross-tenant key: seeing more
/// than one tenant is what acting in the global tenant means (FDW-825).
/// </para>
/// <para>
/// No key lets a user read a Restricted row: those are the system context's alone, and no permission
/// reaches them (FDW-1086).
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlSessionContextTypes), "ForUser")]
public sealed class ForUserSessionContext() : MsSqlSessionContextBase("ForUser")
{
    /// <inheritdoc />
    public override bool Governs(IAuthenticationContext? authenticationContext)
        => IsResolvedUser(authenticationContext);

    /// <inheritdoc />
    public override SessionContextPlan Plan(IAuthenticationContext? authenticationContext)
    {
        if (!IsResolvedUser(authenticationContext))
        {
            throw new InvalidOperationException(
                $"{nameof(ForUserSessionContext)}.{nameof(Plan)} was called with an authentication context it does not govern.");
        }

        return SessionContextPlan.ForUser(
            Guid.Parse(authenticationContext!.UserId),
            authenticationContext.ActiveTenantId);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Bounded, because every branch this context can reach joins a live table: membership in
    /// <c>tenant.UserTenantsConfiguration</c>, org access in <c>tenant.TenantOrgAccess</c>, and
    /// <c>security.VisibilityGroup</c>. Revoking a membership or a grant changes
    /// the next query's answer immediately while the caller's own identity — and therefore the cache
    /// partition — is unchanged, so without a ceiling a revoked user keeps being served the rows they
    /// just lost for as long as the entry lives.
    /// </para>
    /// <para>
    /// Why a ceiling rather than invalidation: nothing in the framework writes these tables, so there
    /// is no <c>Save</c> to hang an <c>ICacheInvalidator</c> call on. Grants change out of band, which
    /// no in-process event can observe. A bound is the only mechanism that does not depend on being
    /// told.
    /// </para>
    /// <para>
    /// The value is a security posture, not a derived fact: how long a revocation may take to bite.
    /// Thirty seconds keeps a revocation effectively prompt while still absorbing the repeated reads
    /// a single request makes. It caps the command's request and never extends it.
    /// </para>
    /// </remarks>
    public override TimeSpan MaxCacheDuration(IAuthenticationContext? authenticationContext)
        => MsSqlSessionContextTypes.RevocationCacheCeiling;

    /// <inheritdoc />
    public override SessionContextStatement Statement(SessionContextPlan plan, string parameterPrefix)
    {
        var userId = string.Concat(parameterPrefix, "userId");
        var sql = $"EXEC sp_set_session_context @key = N'UserId', @value = @{userId}, @read_only = 1;";
        var parameters = new List<SessionContextParameter>
        {
            // Always populated for a non-system plan.
            new(userId, SqlDbType.UniqueIdentifier, plan.UserId!.Value),
        };

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
            MsSqlConnectionLogger.TraceTenantContextSet(logger, plan.TenantId.Value.ToString());
        }
        else
        {
            MsSqlConnectionLogger.TraceTenantContextSet(logger, plan.UserId!.Value.ToString());
        }
    }
}
