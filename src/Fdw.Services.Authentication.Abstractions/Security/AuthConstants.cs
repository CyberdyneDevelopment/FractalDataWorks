using System;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Well-known, reserved <see cref="Guid"/> identities used by the authentication/RLS session-context
/// mechanism. Not a fallback value — this is a deliberate, explicitly user-approved sentinel principal
/// with a fixed, documented meaning, not an "if missing then assume X" default.
/// </summary>
public static class AuthConstants
{
    /// <summary>
    /// The reserved "deny-everywhere" principal <c>UserId</c> that <c>MsSqlConnection</c> sets on
    /// <c>SESSION_CONTEXT('UserId')</c> whenever no real, Guid-identified, authenticated principal is
    /// established for the current call flow (and the call is not an explicit
    /// <see cref="SystemAuthenticationContext"/> elevation).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why a real, non-null Guid rather than leaving <c>UserId</c> unset: <c>security.fn_TenantFilter</c>
    /// (unchanged) treats <c>SESSION_CONTEXT('UserId') IS NULL</c> as Mode 1 — the full-visibility
    /// system bypass. The application must NEVER send a null <c>UserId</c> itself; only the one
    /// deliberate <see cref="SystemAuthenticationContext"/> elevation is allowed to leave it unset.
    /// Every other case — no <see cref="IAuthenticationContext"/> established at all, or one whose
    /// <see cref="IAuthenticationContext.UserId"/> is not a parseable <see cref="Guid"/> — resolves to
    /// THIS reserved principal instead of falling through to the same bypass.
    /// </para>
    /// <para>
    /// This identity holds zero <c>tenant.TenantOrgAccess</c> grants — by construction, no seed or
    /// migration will ever insert a row for it — so every tenant-scoped RLS branch in
    /// <c>fn_TenantFilter</c> denies it. It falls through only to the shared/system-row branch
    /// (<c>TenantId IS NULL AND VisibilityGroupId IS NULL</c>), i.e. "deny everywhere tenant-scoped,"
    /// exactly like any other authenticated-but-tenant-less caller.
    /// </para>
    /// <para>
    /// Deliberately NOT <see cref="Guid.Empty"/> — a value that could plausibly appear by accident
    /// from an uninitialized field or a default struct value. All-<c>F</c>s is obviously reserved and
    /// cannot collide with an app-minted <c>Guid.CreateVersion7()</c> identity.
    /// </para>
    /// </remarks>
    public static readonly Guid NoAccessPrincipalId = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");
}
