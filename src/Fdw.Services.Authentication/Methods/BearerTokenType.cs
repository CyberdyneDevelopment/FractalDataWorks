using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// Bearer token type for HTTP Authorization header.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TokenTypes), "BearerToken", RestrictToCurrentCompilation = true)]
public sealed class BearerTokenType : TokenTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BearerTokenType"/> class.
    /// </summary>
    public BearerTokenType() : base(
        id: 4,
        name: "BearerToken",
        format: "JWT",
        canBeRefreshed: false,
        containsUserIdentity: true,
        typicalLifetimeSeconds: 3600) // 1 hour
    {
    }
}
