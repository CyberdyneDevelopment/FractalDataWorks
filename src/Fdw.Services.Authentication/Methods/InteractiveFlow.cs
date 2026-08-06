using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// Interactive authentication flow with user interaction.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationFlows), "Interactive", RestrictToCurrentCompilation = true)]
public sealed class InteractiveFlow : AuthenticationFlowBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveFlow"/> class.
    /// </summary>
    public InteractiveFlow() : base(
        id: 3,
        name: "Interactive",
        requiresUserInteraction: true,
        supportsRefreshTokens: true,
        isServerToServer: false)
    {
    }
}
