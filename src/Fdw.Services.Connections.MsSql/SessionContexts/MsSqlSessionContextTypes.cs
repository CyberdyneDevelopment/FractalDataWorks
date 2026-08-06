using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// The <b>reference row-level-security scheme's</b> session contexts for SQL Server connections.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is one scheme, not "the MsSql session contexts".</b> Connection <i>kind</i> and
/// session-context <i>scheme</i> are separate axes: two consumers can both talk to SQL Server and
/// disagree completely about what a session context carries. These three options are the reference
/// design expressed as code, and each is a clause-level contract with
/// <c>security.fn_TenantFilter</c> <b>as deployed</b> in <c>databases/ConfigurationDb/security/</c>:
/// </para>
/// <list type="bullet">
///   <item>
///     <b><see cref="SystemContext"/></b> — sets <b>nothing at all</b>. It is elevation only because
///     Mode 1 (<c>fn_TenantFilter.sql:44</c>) keys off <c>SESSION_CONTEXT('UserId') IS NULL</c>:
///     the elevation <i>is</i> the absence of a key. There is no <c>SystemContext</c> key. This is
///     also why "apply no session context" and "full system elevation" are indistinguishable on the
///     wire — a fact any future per-connection participation rule has to reckon with.
///   </item>
///   <item>
///     <b><see cref="ForUser"/></b> — the <c>UserId</c> / <c>TenantId</c> / <c>CrossTenant</c> /
///     <c>CanReadSecrets</c> key set. Four names but at most three keys: <c>TenantId</c> XOR
///     <c>CrossTenant</c>, because Modes 3 and 2 are alternatives, never both. The exclusion is part
///     of the contract, not an implementation detail.
///   </item>
///   <item>
///     <b><see cref="Deny"/></b> — <c>UserId</c> = <see cref="AuthConstants.NoAccessPrincipalId"/>
///     and nothing else. Its deny-ness is <b>emergent, not declared</b>: that Guid appears nowhere in
///     <c>databases/</c> — no predicate, no policy, no seed row knows it. It denies because it holds
///     zero rows in <c>tenant.TenantOrgAccess</c>. A different scheme gets a silently different
///     verdict from the identical option.
///   </item>
/// </list>
/// <para>
/// <b><see cref="Deny"/> is not deny-<i>everywhere</i>.</b> <c>fn_TenantFilter.sql:48-51</c> admits
/// shared/system rows (<c>TenantId IS NULL AND VisibilityGroupId IS NULL</c>) with no session-context
/// test at all. The deny principal sees exactly those, same as any other tenant-less caller. It is
/// denied every <i>tenant-scoped</i> branch, which is a narrower claim.
/// </para>
/// <para>
/// <b>Replacing this collection replaces that contract wholesale</b> — including the system-bypass
/// behaviour, which is a property of Mode 1 specifically and not of session contexts in general. A
/// consumer running a different predicate, different key names, a tenancy model that is not
/// tenant+org, or an approach that is not <c>SESSION_CONTEXT</c> at all does not want these three
/// options adjusted; they want a different list. So they point their connection type's
/// <c>SessionContextTypes</c> at their own collection and inherit none of these assumptions, rather
/// than adding a fourth option that must coexist with three that mean things they did not choose.
/// </para>
/// <para>
/// Correspondingly, <b>no code outside this package may read <c>SessionContextTypes</c> assuming
/// these members exist</b> — no <c>ByName("SystemContext")</c>, no <c>First(x =&gt; x.Name == "Deny")</c>,
/// no switch on member names. Naming them is legitimate only here, in the package that owns the
/// scheme.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(MsSqlSessionContextBase), typeof(ISessionContext), typeof(MsSqlSessionContextTypes))]
public abstract partial class MsSqlSessionContextTypes : TypeCollectionBase<MsSqlSessionContextBase, ISessionContext>
{
    /// <summary>
    /// Gets the session context that governs <paramref name="authenticationContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the scheme's selection rule, and it lives here because it is part of the scheme: a
    /// consumer replacing the collection replaces this with it.
    /// </para>
    /// <para>
    /// Why <c>Single</c> and not <c>First</c>, and why there is no "nothing matched" branch:
    /// <see cref="MsSqlSessionContextBase.Governs"/> partitions the space of authentication contexts
    /// exhaustively and exclusively — <see cref="Deny"/>'s predicate is the explicit complement of
    /// the other two, so exactly one option governs any input including <see langword="null"/>. The
    /// selection is therefore order-independent (registration order of a TypeCollection is not a
    /// contract) and needs no fallback. <c>Single</c> states that invariant rather than quietly
    /// picking a winner if a future edit broke it.
    /// </para>
    /// </remarks>
    /// <param name="authenticationContext">
    /// The authentication context of the current logical call flow, or <see langword="null"/> when
    /// none has been established.
    /// </param>
    public static MsSqlSessionContextBase For(IAuthenticationContext? authenticationContext)
        => All().OfType<MsSqlSessionContextBase>().Single(c => c.Governs(authenticationContext));
}
