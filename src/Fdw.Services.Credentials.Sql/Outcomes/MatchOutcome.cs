using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Credentials.Abstractions.Outcomes;

namespace Fdw.Services.Credentials.Sql.Outcomes;

/// <summary>
/// The presented credential matched the stored secret. The ONLY outcome that grants access; produced
/// by the vault's constant-time compare.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption — no logic to test.</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CredentialOutcomes), "Match")]
public sealed class MatchOutcome : CredentialOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="MatchOutcome"/> class.</summary>
    public MatchOutcome() : base(1, "Match", grantsAccess: true) { }
}
