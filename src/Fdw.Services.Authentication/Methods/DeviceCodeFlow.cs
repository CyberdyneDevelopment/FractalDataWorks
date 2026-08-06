using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// OAuth 2.0 Device Code flow.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationFlows), "DeviceCode", RestrictToCurrentCompilation = true)]
public sealed class DeviceCodeFlow : AuthenticationFlowBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceCodeFlow"/> class.
    /// </summary>
    public DeviceCodeFlow() : base(
        id: 4,
        name: "DeviceCode",
        requiresUserInteraction: true,
        supportsRefreshTokens: true,
        isServerToServer: false)
    {
    }
}
