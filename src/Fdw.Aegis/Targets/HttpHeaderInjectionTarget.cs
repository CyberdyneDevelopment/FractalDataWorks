using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Logging;
using Fdw.Results;
using Fdw.Services.SecretManagers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Aegis.Targets;

/// <summary>
/// The Phase 1 <see cref="IAegisInjectionTarget"/>: injects the resolved secret as an outbound HTTP
/// <c>Authorization: Bearer</c> header. Generalizes <c>ConfiguredMcpClientDelegate.CreateTransport</c>'s
/// <c>env[TargetEnvironmentVariable]=token</c> injection (mcp-hub) from a spawned process's
/// environment to an HTTP request header.
/// </summary>
/// <remarks>
/// Why a raw <see cref="IHttpClientFactory"/> here is in-pattern (not a DataGateway violation): this
/// is the brokered downstream ACTION Aegis exists to gate — the egress point — exactly like
/// <c>ConfiguredMcpClientDelegate</c>, which also does not route through <c>IDataGateway</c>. The
/// DataGateway-only rule governs FDW data access, not an arbitrary downstream HTTP call this
/// injector is approved to make on Claude's behalf.
/// </remarks>
public sealed class HttpHeaderInjectionTarget : IAegisInjectionTarget
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpHeaderInjectionTarget> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHeaderInjectionTarget"/> class.
    /// </summary>
    public HttpHeaderInjectionTarget(IHttpClientFactory httpClientFactory, ILogger<HttpHeaderInjectionTarget>? logger = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? NullLogger<HttpHeaderInjectionTarget>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AegisInjectionOutcome>> Inject(
        ApprovalRequest request, SecretValue secret, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(request.ConnectionName);

        var token = secret.GetStringValue();
        if (!IsValidHeaderValue(token))
            return GenericResult<AegisInjectionOutcome>.Failure(
                AegisLog.InjectionFailed(_logger, request.CommandName, "resolved secret is not a valid HTTP header value"));

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, string.Empty);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return GenericResult<AegisInjectionOutcome>.Failure(
                    AegisLog.InjectionFailed(_logger, request.CommandName, $"downstream returned {(int)response.StatusCode}"));
            }

            return GenericResult<AegisInjectionOutcome>.Success(new AegisInjectionOutcome
            {
                Success = true,
                CorrelationId = request.CorrelationId,
                Reference = $"status={(int)response.StatusCode};body={Fingerprint(body)}",
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            return GenericResult<AegisInjectionOutcome>.Failure(AegisLog.InjectionFailed(_logger, request.CommandName, ex.Message));
        }
    }

    private static bool IsValidHeaderValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var c in value)
        {
            if (char.IsControl(c))
                return false;
        }

        return true;
    }

    private static string Fingerprint(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..12].ToLowerInvariant();
}
