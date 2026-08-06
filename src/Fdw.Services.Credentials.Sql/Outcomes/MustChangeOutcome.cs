using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Credentials.Abstractions.Outcomes;

namespace Fdw.Services.Credentials.Sql.Outcomes;

/// <summary>
/// The credential matched but the user must change it before proceeding (composed by the edge from the
/// non-secret <c>MustChangePasswordOnLogin</c> flag). Denies access until changed.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption — no logic to test.</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CredentialOutcomes), "MustChange")]
public sealed class MustChangeOutcome : CredentialOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="MustChangeOutcome"/> class.</summary>
    public MustChangeOutcome() : base(5, "MustChange", grantsAccess: false) { }
}
