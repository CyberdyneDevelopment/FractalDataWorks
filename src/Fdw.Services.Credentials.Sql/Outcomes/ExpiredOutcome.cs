using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Credentials.Abstractions.Outcomes;

namespace Fdw.Services.Credentials.Sql.Outcomes;

/// <summary>
/// The credential matched but has expired (composed by the edge from non-secret policy metadata —
/// <c>LastPasswordChangedAt</c> + <c>PasswordMaxAgeDays</c>). Denies access; forces a change.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption — no logic to test.</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CredentialOutcomes), "Expired")]
public sealed class ExpiredOutcome : CredentialOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="ExpiredOutcome"/> class.</summary>
    public ExpiredOutcome() : base(3, "Expired", grantsAccess: false) { }
}
