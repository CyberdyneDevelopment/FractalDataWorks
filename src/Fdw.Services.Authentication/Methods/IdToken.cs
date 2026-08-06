using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// OpenID Connect ID Token type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TokenTypes), "IdToken", RestrictToCurrentCompilation = true)]
public sealed class IdToken : TokenTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdToken"/> class.
    /// </summary>
    public IdToken() : base(
        id: 2,
        name: "IdToken",
        format: "JWT",
        canBeRefreshed: false,
        containsUserIdentity: true,
        typicalLifetimeSeconds: 3600) // 1 hour
    {
    }
}
