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
}
