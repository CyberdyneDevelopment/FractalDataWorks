namespace Fdw.UI.Blazor.Authentication.Components;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Clients;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.UI.Blazor.Authentication.Models;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Headless login form component. Manages login state and delegates rendering
/// to the consuming application via <see cref="FormContent"/>.
/// </summary>
public sealed partial class FdwLoginForm : ComponentBase, IDisposable
{
    [Inject]
    private IAuthenticationClient AuthClient { get; set; } = default!;

    /// <summary>
    /// Gets or sets the render fragment that defines the form's visual appearance.
    /// Receives a <see cref="LoginFormContext"/> with the form model, state, and submit callback.
    /// </summary>
    [Parameter]
    public RenderFragment<LoginFormContext>? FormContent { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when login completes (success or failure).
    /// </summary>
    [Parameter]
    public EventCallback<AuthResult> OnLoginResult { get; set; }

    private readonly LoginRequest _model = new();
    private bool _isLoading;
    private string? _errorMessage;
    private LoginFormContext _context = default!;
    private CancellationTokenSource? _cts;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _context = CreateContext();
    }

    private async Task HandleSubmit()
    {
        _errorMessage = null;
        _isLoading = true;
        _context = CreateContext();
        StateHasChanged();

        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        _cts = new CancellationTokenSource();

        try
        {
            var result = await AuthClient.Login(_model, _cts.Token);

            if (!result.Success)
            {
                _errorMessage = result.ErrorMessage;
            }

            await OnLoginResult.InvokeAsync(result);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == _cts?.Token)
        {
            // Cancelled by the component — silent return.
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isLoading = false;
            _context = CreateContext();
            await InvokeAsync(StateHasChanged);
        }
    }

    private LoginFormContext CreateContext()
    {
        return new LoginFormContext(
            _model,
            _isLoading,
            _errorMessage,
            EventCallback.Factory.Create(this, HandleSubmit));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts?.Dispose();
    }
}
