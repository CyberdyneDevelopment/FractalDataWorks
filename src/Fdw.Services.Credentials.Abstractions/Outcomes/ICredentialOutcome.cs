using Fdw.Collections;

namespace Fdw.Services.Credentials.Abstractions.Outcomes;

/// <summary>
/// A credential validation outcome — the result of comparing a presented credential against the
/// vault and (at the edge) applying non-secret policy. Extends <see cref="ITypeOption{TKey}"/> so the
/// outcomes form a downstream-extensible <see cref="CredentialOutcomes"/> TypeCollection rather than a
/// closed enum or a bare <c>bool</c> (see DataVault README §5).
/// </summary>
/// <remarks>
/// The vault only ever produces the compare outcomes (<c>Match</c>/<c>NoMatch</c>); the edge composes
/// <c>Expired</c>/<c>TooManyAttempts</c>/<c>MustChange</c> from non-secret policy metadata. Every
/// option except <c>Match</c> denies access.
/// </remarks>
public interface ICredentialOutcome : ITypeOption<int>
{
    /// <summary>
    /// Gets a value indicating whether this outcome, on its own, grants access. Only <c>Match</c>
    /// returns <c>true</c>; every policy-denial outcome returns <c>false</c>.
    /// </summary>
    bool GrantsAccess { get; }

    /// <summary>
    /// Gets a value indicating whether the presented secret was CORRECT — whatever policy then
    /// decided. True for <c>Match</c>, and for <c>Expired</c> and <c>MustChange</c>, which are only
    /// composed after the secret matched; false for <c>NoMatch</c> and <c>TooManyAttempts</c>.
    /// </summary>
    /// <remarks>
    /// Changing a password needs proof of the current one, not permission to sign in with it: a user
    /// whose password is due for change has to be able to change it, and <see cref="GrantsAccess"/>
    /// is false for exactly that user.
    /// </remarks>
    bool ProvesSecret { get; }
}
