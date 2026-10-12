using System;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>Result of presenting a set-password link's token (FDW-941).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class PasswordLinkRedemption
{
    /// <summary>Gets or sets whether the token named a live link, which this redemption has now used up.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets the user whose password the link sets, when valid.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the link's id, when valid.</summary>
    public Guid LinkId { get; set; }
}
