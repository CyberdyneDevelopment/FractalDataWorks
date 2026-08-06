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
public abstract class CredentialOutcomeBase : TypeOptionBase<int, CredentialOutcomeBase>, ICredentialOutcome
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialOutcomeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this outcome.</param>
    /// <param name="name">The name of this outcome.</param>
    /// <param name="grantsAccess">Whether this outcome, on its own, grants access.</param>
    protected CredentialOutcomeBase(int id, string name, bool grantsAccess)
        : base(id, name)
    {
        GrantsAccess = grantsAccess;
    }

    /// <inheritdoc />
    public bool GrantsAccess { get; }
}
