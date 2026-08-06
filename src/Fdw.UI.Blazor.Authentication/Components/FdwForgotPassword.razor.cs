namespace Fdw.UI.Blazor.Authentication.Components;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Clients;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.UI.Blazor.Authentication.Models;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Headless forgot-password component. Provider-aware: for OIDC providers it will
/// expose a redirect URL; for local providers it will show a success confirmation.
/// </summary>
public sealed partial class FdwForgotPassword : ComponentBase, IDisposable
{
    [Inject]
    private IForgotPasswordProvider ForgotPasswordProvider { get; set; } = default!;

    /// <summary>
    /// Gets or sets the render fragment that defines the component's visual appearance.
    /// Receives a <see cref="ForgotPasswordContext"/> with model, state, and submit callback.
    /// </summary>
    [Parameter]
    public RenderFragment<ForgotPasswordContext>? Content { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the forgot password operation completes.
    /// </summary>
    [Parameter]
    public EventCallback<ForgotPasswordResult> OnResult { get; set; }

    private string _identifier = "";
    private bool _isLoading;
    private string? _errorMessage;
    private bool _isSuccess;
    private string? _redirectUrl;
    private ForgotPasswordContext _context = default!;
    private CancellationTokenSource? _cts;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _context = CreateContext();
    }

    private async Task HandleSubmit()
    {
        _errorMessage = null;
        _isSuccess = false;
        _redirectUrl = null;
        _isLoading = true;
        _identifier = _context.Identifier;
        _context = CreateContext();
        StateHasChanged();

        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
        }

        _cts = new CancellationTokenSource();

        try
        {
            var result = await ForgotPasswordProvider.RequestPasswordReset(_identifier, _cts.Token)
                .ConfigureAwait(false);

            if (result.Success)
            {
                _isSuccess = true;
                _redirectUrl = result.RedirectUrl;
            }
            else
            {
                _errorMessage = result.ErrorMessage;
            }

            await OnResult.InvokeAsync(result).ConfigureAwait(false);
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
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private ForgotPasswordContext CreateContext()
    {
        return new ForgotPasswordContext(
            _identifier,
            _isLoading,
            _errorMessage,
            _isSuccess,
            _redirectUrl,
            EventCallback.Factory.Create(this, HandleSubmit));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts?.Dispose();
    }
}
