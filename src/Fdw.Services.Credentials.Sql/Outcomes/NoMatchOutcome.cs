using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Credentials.Abstractions.Outcomes;

namespace Fdw.Services.Credentials.Sql.Outcomes;

/// <summary>
/// The presented credential did not match the stored secret (or no secret is on file — the negative
/// path does the same constant-time work). Produced by the vault's compare; denies access.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption — no logic to test.</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CredentialOutcomes), "NoMatch")]
public sealed class NoMatchOutcome : CredentialOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="NoMatchOutcome"/> class.</summary>
    public NoMatchOutcome() : base(2, "NoMatch", grantsAccess: false) { }
}
