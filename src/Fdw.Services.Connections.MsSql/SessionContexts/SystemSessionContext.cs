using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// Explicit system elevation: sets <b>no <c>SESSION_CONTEXT</c> keys at all</b>.
/// </summary>
/// <remarks>
/// <para>
/// The elevation is the <i>absence</i> of a key. <c>security.fn_TenantFilter</c>'s Mode 1
/// (<c>fn_TenantFilter.sql:44</c>) grants full visibility when <c>SESSION_CONTEXT('UserId') IS NULL</c>;
/// there is no dedicated <c>SystemContext</c> key to set. This is the only option that leaves
/// <c>UserId</c> unset — every other one sets a real Guid or the reserved deny principal, so an
/// anonymous or unestablished caller can never reach the bypass by omission.
/// </para>
/// <para>
/// A direct consequence: "this connection applies no session context" and "this connection is fully
/// system-elevated" are <b>the same bytes on the wire</b>. Nothing downstream can tell them apart,
/// which is why a per-connection participation rule cannot treat "not supported" as a benign value.
/// </para>
/// <para>
/// Named <c>SystemContext</c> rather than <c>System</c>: a TypeCollection member named <c>System</c>
/// would shadow the <c>System</c> namespace inside the generated collection body, which emits
/// unqualified <c>System.Type</c> and <c>System.Threading.Tasks.Task.CompletedTask</c>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlSessionContextTypes), "SystemContext")]
public sealed class SystemSessionContext() : MsSqlSessionContextBase(1, "SystemContext")
{
    /// <inheritdoc />
    public override bool Governs(IAuthenticationContext? authenticationContext)
        => IsSystemElevation(authenticationContext);

    /// <inheritdoc />
    public override SessionContextPlan Plan(IAuthenticationContext? authenticationContext)
        => SessionContextPlan.System;

    /// <inheritdoc />
    /// <remarks>
    /// Unbounded. Mode 1 grants full visibility on <c>SESSION_CONTEXT('UserId') IS NULL</c> alone
    /// (<c>fn_TenantFilter.sql:44</c>) — it joins no table and consults no grant, so no edit to
    /// authorization data can narrow or widen what this context sees. There is nothing to go stale.
    /// </remarks>
    public override TimeSpan MaxCacheDuration(IAuthenticationContext? authenticationContext)
        => TimeSpan.MaxValue;

    /// <inheritdoc />
    public override Task Apply(
        SqlConnection connection,
        SessionContextPlan plan,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        MsSqlConnectionLogger.TraceSystemBypassConnectionUsed(logger);
        return Task.CompletedTask;
    }
}
