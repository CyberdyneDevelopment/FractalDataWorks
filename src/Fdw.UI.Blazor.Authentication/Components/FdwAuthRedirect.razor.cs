namespace Fdw.UI.Blazor.Authentication.Components;

using System;
using Fdw.Services.Authentication.Clients;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Headless component that redirects unauthenticated users to a login page,
/// preserving the return URL. Renders child content only when authenticated.
/// </summary>
public sealed partial class FdwAuthRedirect : ComponentBase, IDisposable
{
    [Inject]
    private IAuthenticationClient AuthClient { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the login page URL. Defaults to "/login".
    /// </summary>
    [Parameter]
    public string LoginUrl { get; set; } = "/login";

    /// <summary>
    /// Gets or sets the query parameter name for the return URL. Defaults to "returnUrl".
    /// </summary>
    [Parameter]
    public string ReturnUrlParameter { get; set; } = "returnUrl";

    /// <summary>
    /// Gets or sets the child content to render when authenticated.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool _isAuthenticated;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _isAuthenticated = AuthClient.IsAuthenticated;
        AuthClient.AuthStateChanged += OnAuthStateChanged;

        if (!_isAuthenticated)
        {
            RedirectToLogin();
        }
    }

    private void OnAuthStateChanged(object? sender, AuthStateChangedEventArgs e)
    {
        _isAuthenticated = e.User is not null;

        if (!_isAuthenticated)
        {
            RedirectToLogin();
        }

        _ = InvokeAsync(StateHasChanged);
    }

    private void RedirectToLogin()
    {
        var currentUri = Navigation.Uri;
        var baseUri = Navigation.BaseUri;
        var relativePath = currentUri;

        if (currentUri.StartsWith(baseUri, StringComparison.Ordinal))
        {
            relativePath = currentUri.Substring(baseUri.Length);
        }

        var loginUrl = $"{LoginUrl}?{ReturnUrlParameter}={Uri.EscapeDataString(relativePath)}";
        Navigation.NavigateTo(loginUrl);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        AuthClient.AuthStateChanged -= OnAuthStateChanged;
    }
}
