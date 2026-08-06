using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// OAuth 2.0 Access Token type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TokenTypes), "AccessToken", RestrictToCurrentCompilation = true)]
public sealed class AccessToken : TokenTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccessToken"/> class.
    /// </summary>
    public AccessToken() : base(
        id: 1,
        name: "AccessToken",
        format: "JWT",
        canBeRefreshed: false,
        containsUserIdentity: false,
        typicalLifetimeSeconds: 3600) // 1 hour
    {
    }
}
