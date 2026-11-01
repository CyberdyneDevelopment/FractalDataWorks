using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Credentials.Abstractions.Outcomes;

/// <summary>
/// Base class for credential outcome type options. Concrete options (Match / NoMatch / Expired /
/// TooManyAttempts / MustChange) live in the implementation library and are therefore
/// downstream-extensible.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class — no logic to test.</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class CredentialOutcomeBase : TypeOptionBase<CredentialOutcomeBase>, ICredentialOutcome
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialOutcomeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this outcome.</param>
    /// <param name="grantsAccess">Whether this outcome, on its own, grants access.</param>
    /// <param name="provesSecret">Whether the presented secret was correct.</param>
    protected CredentialOutcomeBase(string name, bool grantsAccess, bool provesSecret)
        : base(name)
    {
        GrantsAccess = grantsAccess;
        ProvesSecret = provesSecret;
    }

    /// <inheritdoc />
    public bool GrantsAccess { get; }

    /// <inheritdoc />
    public bool ProvesSecret { get; }
}
