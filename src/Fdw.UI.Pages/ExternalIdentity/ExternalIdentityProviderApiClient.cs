namespace Fdw.Services.ExternalIdentityProviders.Clients;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.ExternalIdentityProviders.Abstractions.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for login-time external identity provider discovery. Lets the UI (which never opens
/// ConfigurationDb) obtain the active providers' public login subset from the API. Called server-side by
/// the Blazor host with its own service identity — the endpoint is role-gated
/// (<c>identityproviders:read</c>) and consumed before any end-user token exists.
/// </summary>
public sealed class ExternalIdentityProviderApiClient : ApiClientBase
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProviderApiClient"/> class.</summary>
    public ExternalIdentityProviderApiClient(HttpClient httpClient, ILogger<ExternalIdentityProviderApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets the active external identity providers' public login-discovery subset (name, type, authority,
    /// clientId, discovery URL) — never any secret.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of provider summaries.</returns>
    public Task<IGenericResult<IReadOnlyList<ExternalIdentityProviderSummaryDto>>> GetProviders(CancellationToken ct = default)
        => GetList<ExternalIdentityProviderSummaryDto>("auth/external-identity-providers", ct);
}
