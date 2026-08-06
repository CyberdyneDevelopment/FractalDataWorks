namespace Fdw.UI.Blazor.Authentication.Components;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Clients;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Headless logout button component. Wraps the provided child content
/// and handles logout on click, with optional redirect.
/// </summary>
public sealed partial class FdwLogoutButton : ComponentBase, IDisposable
{
    [Inject]
    private IAuthenticationClient AuthClient { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the child content rendered inside the clickable area.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the URL to redirect to after logout. If <c>null</c>, no redirect occurs.
    /// </summary>
    [Parameter]
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked after logout completes.
    /// </summary>
    [Parameter]
    public EventCallback OnLoggedOut { get; set; }

    private CancellationTokenSource? _cts;

    private async Task HandleLogout()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
        }

        _cts = new CancellationTokenSource();

        try
        {
            await AuthClient.Logout(_cts.Token).ConfigureAwait(false);
            await OnLoggedOut.InvokeAsync().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(RedirectUrl))
            {
                Navigation.NavigateTo(RedirectUrl);
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == _cts?.Token)
        {
            // Cancelled by the component — silent return.
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts?.Dispose();
    }
}
