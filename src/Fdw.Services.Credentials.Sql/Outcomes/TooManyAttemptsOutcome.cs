using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Credentials.Abstractions.Outcomes;

namespace Fdw.Services.Credentials.Sql.Outcomes;

/// <summary>
/// The account is temporarily locked after too many consecutive failures (composed by the edge from a
/// non-secret failure counter). Denies access; the vault is never the rate-limiter.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption — no logic to test.</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CredentialOutcomes), "TooManyAttempts")]
public sealed class TooManyAttemptsOutcome : CredentialOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="TooManyAttemptsOutcome"/> class.</summary>
    public TooManyAttemptsOutcome() : base(4, "TooManyAttempts", grantsAccess: false) { }
}
