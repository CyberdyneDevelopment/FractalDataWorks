namespace Fdw.Web.Http.Authentication.Blazor;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Http.Authentication.Blazor.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Scoped <see cref="CircuitHandler"/> that captures the access token from HttpContext when the
/// circuit connects (WebSocket handshake -- HttpContext is still valid) and flows it into an
/// <see cref="AsyncLocal{T}"/> via <see cref="CircuitTokenAccessor"/> before every inbound event.
/// </summary>
public sealed class TokenCapturingCircuitHandler : CircuitHandler
{
    private readonly CircuitTokenAccessor _accessor;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TokenCapturingCircuitHandler> _logger;
    private string? _capturedToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenCapturingCircuitHandler"/> class.
    /// </summary>
    /// <param name="accessor">The circuit token accessor.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="logger">The logger.</param>
    public TokenCapturingCircuitHandler(
        CircuitTokenAccessor accessor,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TokenCapturingCircuitHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _accessor = accessor;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            _capturedToken = await httpContext.GetTokenAsync("access_token").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(_capturedToken))
            {
                BlazorAuthLog.TokenCaptured(_logger, circuit.Id);
            }
            else
            {
                BlazorAuthLog.TokenNotFound(_logger, circuit.Id);
            }
        }
        else
        {
            BlazorAuthLog.NoHttpContext(_logger, circuit.Id);
        }

        await base.OnConnectionUpAsync(circuit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            _accessor.CurrentToken = _capturedToken;
            try
            {
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                _accessor.CurrentToken = null;
            }
        };
    }

    /// <inheritdoc />
    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _capturedToken = null;
        BlazorAuthLog.CircuitClosed(_logger, circuit.Id);
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }
}
