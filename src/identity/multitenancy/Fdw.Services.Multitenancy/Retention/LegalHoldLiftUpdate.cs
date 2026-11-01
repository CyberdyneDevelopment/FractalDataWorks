using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// The columns changed when a legal hold is lifted (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class LegalHoldLiftUpdate
{
    /// <summary>Gets or sets the user who lifted the hold.</summary>
    public string LiftedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets when the hold was lifted.</summary>
    public DateTimeOffset LiftedAt { get; set; }
}
