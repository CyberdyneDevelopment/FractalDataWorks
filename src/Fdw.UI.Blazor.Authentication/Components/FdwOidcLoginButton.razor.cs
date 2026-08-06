namespace Fdw.UI.Blazor.Authentication.Components;

using System;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Headless component that renders a link to trigger an OIDC authentication challenge.
/// The consuming app provides the button content via <see cref="ChildContent"/>.
/// </summary>
public sealed partial class FdwOidcLoginButton : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the authentication scheme name (e.g., "Authentik", "AzureAd").
    /// </summary>
    [Parameter]
    public string Scheme { get; set; } = "";

    /// <summary>
    /// Gets or sets the challenge endpoint path. Defaults to "/auth/challenge".
    /// </summary>
    [Parameter]
    public string ChallengePath { get; set; } = "/auth/challenge";

    /// <summary>
    /// Gets or sets the URL to redirect to after successful authentication.
    /// Defaults to "/".
    /// </summary>
    [Parameter]
    public string ReturnUrl { get; set; } = "/";

    /// <summary>
    /// Gets or sets the child content rendered inside the link.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string _challengeUrl = "";

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _challengeUrl = $"{ChallengePath}?scheme={Uri.EscapeDataString(Scheme)}&returnUrl={Uri.EscapeDataString(ReturnUrl)}";
    }
}
