using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// A real, authenticated, <see cref="Guid"/>-identified user: sets the
/// <c>UserId</c> / <c>TenantId</c> / <c>CrossTenant</c> / <c>CanReadSecrets</c> key set that
/// <c>security.fn_TenantFilter</c> reads.
/// </summary>
/// <remarks>
/// <para>
/// Four names but <b>at most three keys</b>. <c>TenantId</c> and <c>CrossTenant</c> are mutually
/// exclusive: Mode 3 (strict single active tenant) and Mode 2 (cross-tenant authorized) are
/// alternatives in the predicate, never both. That exclusion is part of the contract, not an
/// implementation detail — setting both would put the connection in a state the deployed predicate
/// has no branch for.
/// </para>
/// <para>
/// <c>CanReadSecrets</c> is set only when the caller's token actually carries
/// <c>connections:read-secrets</c>. Absence keeps the default (Mode 4 restricted system rows stay
/// hidden) with no fallback.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlSessionContextTypes), "ForUser")]
public sealed class ForUserSessionContext() : MsSqlSessionContextBase(2, "ForUser")
{
    // Why: matches the permission checked by the security.fn_TenantFilter RLS predicate that reads
    // SESSION_CONTEXT('CanReadSecrets') to show restricted system rows (Mode 4). Keep this const in
    // sync with the fn_TenantFilter predicate in the databases repo.
    private const string ReadSecretsPermission = "connections:read-secrets";

    // Why: the longest a result read under a live-grant-joining branch may outlive the grant that
    // permitted it. Keep this in sync with the security posture, not with the gateway's own default.
    private static readonly TimeSpan RevocationCeiling = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public override bool Governs(IAuthenticationContext? authenticationContext)
        => IsResolvedUser(authenticationContext);

    /// <inheritdoc />
    public override SessionContextPlan Plan(IAuthenticationContext? authenticationContext)
    {
        // Why not a fallback: Governs already established this context is a Guid-identified,
        // authenticated, non-system user, so the parse cannot fail here. Plan is only ever called
        // for the context that governs it.
        if (!IsResolvedUser(authenticationContext))
        {
            throw new InvalidOperationException(
                $"{nameof(ForUserSessionContext)}.{nameof(Plan)} was called with an authentication context it does not govern.");
        }

        return SessionContextPlan.ForUser(
            Guid.Parse(authenticationContext!.UserId),
            authenticationContext.ActiveTenantId,
            authenticationContext.IsCrossTenant,
            authenticationContext.Permissions.Contains(ReadSecretsPermission, StringComparer.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Bounded, because every branch this context can reach joins a live table. Modes 2 and 3 test
    /// <c>tenant.TenantOrgAccess</c> for the calling user, and Modes 2 and 4 test
    /// <c>security.VisibilityGroup</c> (<c>fn_TenantFilter.sql:61-123</c>). Revoking a grant changes
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
        => RevocationCeiling;

    /// <inheritdoc />
    public override async Task Apply(
        SqlConnection connection,
        SessionContextPlan plan,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var userId = plan.UserId!.Value; // Always populated for a non-system plan.

        var sql = "EXEC sp_set_session_context @key = N'UserId', @value = @userId, @read_only = 1;";
        var parameters = new List<SqlParameter>
        {
            new("@userId", SqlDbType.UniqueIdentifier) { Value = userId },
        };

        // Why the XOR: Modes 3 and 2 are alternatives in security.fn_TenantFilter — a strict
        // single-tenant caller sets TenantId, a cross-tenant-authorized caller sets CrossTenant, and
        // no caller sets both.
        if (plan.TenantId.HasValue)
        {
            sql += " EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 1;";
            parameters.Add(new SqlParameter("@tenantId", SqlDbType.UniqueIdentifier) { Value = plan.TenantId.Value });
        }
        else if (plan.IsCrossTenant)
        {
            sql += " EXEC sp_set_session_context @key = N'CrossTenant', @value = N'1', @read_only = 1;";
        }

        if (plan.CanReadSecrets)
        {
            sql += " EXEC sp_set_session_context @key = N'CanReadSecrets', @value = N'1', @read_only = 1;";
        }

        await Execute(connection, sql, parameters, logger, cancellationToken).ConfigureAwait(false);

        if (plan.TenantId.HasValue)
        {
            MsSqlConnectionLogger.TraceTenantContextSet(logger, plan.TenantId.Value.ToString());
        }
        else if (plan.IsCrossTenant)
        {
            MsSqlConnectionLogger.TraceCrossTenantContextSet(logger, userId.ToString());
        }
        else
        {
            MsSqlConnectionLogger.TraceTenantContextSet(logger, userId.ToString());
        }

        if (plan.CanReadSecrets)
        {
            MsSqlConnectionLogger.TraceCanReadSecretsContextSet(logger);
        }
    }
}
