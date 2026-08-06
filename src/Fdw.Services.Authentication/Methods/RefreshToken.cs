using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Authentication;

/// <summary>
/// OAuth 2.0 Refresh Token type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TokenTypes), "RefreshToken", RestrictToCurrentCompilation = true)]
public sealed class RefreshToken : TokenTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshToken"/> class.
    /// </summary>
    public RefreshToken() : base(
        id: 3,
        name: "RefreshToken",
        format: "Opaque",
        canBeRefreshed: true,
        containsUserIdentity: false,
        typicalLifetimeSeconds: 2592000) // 30 days
    {
    }
}
