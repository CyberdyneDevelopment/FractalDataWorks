using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// What an OIDC authority row states about its callback address, once its source has checked it (FDW-5730).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record OidcRedirectAddress
{
    /// <summary>Gets the name of the source the row names, as <see cref="OidcRedirectAddressSources"/> knows it.</summary>
    public required string Source { get; init; }

    /// <summary>Gets the row's RedirectUri: absolute for <c>FixedAddress</c>, the root-relative callback path for <c>RequestHost</c>.</summary>
    public required Uri RedirectUri { get; init; }

    /// <summary>Gets the origins a <c>RequestHost</c> callback may be built on; empty for a source that reads none.</summary>
    public required IReadOnlyList<Uri> AllowedOrigins { get; init; }
}
