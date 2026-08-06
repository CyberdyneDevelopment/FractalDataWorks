using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.ExternalIdentityProviders;
using Fdw.Services.ExternalIdentityProviders.Abstractions.Models;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders.Endpoints;

/// <summary>
/// Generic base endpoint for login-time discovery of the active external identity providers. Returns the
/// public <see cref="ExternalIdentityProviderSummaryDto"/> subset (no secrets) so a UI that cannot open
/// ConfigurationDb can render login options and start the browser authorization flow. Reads through the
/// gateway-backed <see cref="ExternalIdentityProviderConfigurationProvider"/>, which only the API tier holds.
/// </summary>
/// <remarks>
/// The all-items <c>Get()</c> is header-only by design (FDW-558), so each active header is re-read via
/// <c>Get(name)</c> to compose its typed body before projecting the public OIDC fields. Fails loud — a
/// read/compose failure returns 500, never a partial or defaulted list. The concrete host subclass sets
/// the auth policy (see <see cref="ConfigureEndpoint"/>): this is consumed pre-user-login, so the caller
/// is the UI's own service identity holding <c>identityproviders:read</c>, never an end-user token.
/// </remarks>
public abstract class GetExternalIdentityProvidersEndpointBase
    : EndpointWithoutRequest<IReadOnlyList<ExternalIdentityProviderSummaryDto>>
{
    private readonly ExternalIdentityProviderConfigurationProvider _configurationProvider;

    /// <summary>Initializes a new instance of the <see cref="GetExternalIdentityProvidersEndpointBase"/> class.</summary>
    protected GetExternalIdentityProvidersEndpointBase(ExternalIdentityProviderConfigurationProvider configurationProvider)
    {
        _configurationProvider = configurationProvider;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/auth/external-identity-providers");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (auth policy, tags, summary).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var headersResult = await _configurationProvider.Get(ct).ConfigureAwait(false);
        if (!headersResult.IsSuccess || headersResult.Value is null)
        {
            ExternalIdentityProviderLog.ProviderDiscoveryFailed(logger, "could not read active external identity provider configurations.");
            await Send.ResponseAsync(new List<ExternalIdentityProviderSummaryDto>(), StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
            return;
        }

        var summaries = new List<ExternalIdentityProviderSummaryDto>(headersResult.Value.Count);
        foreach (var header in headersResult.Value)
        {
            // Why: the list read is header-only (FDW-558); re-read by name to compose the typed body
            // whose public fields (Authority/ClientId) the login page needs. Fail loud on any miss.
            var composedResult = await _configurationProvider.Get(header.Name, ct).ConfigureAwait(false);
            if (!composedResult.IsSuccess || composedResult.Value is null)
            {
                ExternalIdentityProviderLog.ProviderDiscoveryFailed(logger, $"could not compose configuration for provider '{header.Name}'.");
                await Send.ResponseAsync(new List<ExternalIdentityProviderSummaryDto>(), StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
                return;
            }

            var composed = composedResult.Value;
            if (string.IsNullOrEmpty(composed.ServiceOptionType))
            {
                ExternalIdentityProviderLog.ProviderDiscoveryFailed(logger, $"provider '{header.Name}' has no ServiceOptionType discriminator.");
                await Send.ResponseAsync(new List<ExternalIdentityProviderSummaryDto>(), StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
                return;
            }

            // Why: a composed header with no typed body is a genuine compose failure, not an empty
            // provider — fail loud rather than emitting a summary of nulls (FDW-624).
            if (composed.Configuration is null)
            {
                ExternalIdentityProviderLog.ProviderDiscoveryFailed(logger, $"provider '{header.Name}' composed no typed configuration body.");
                await Send.ResponseAsync(new List<ExternalIdentityProviderSummaryDto>(), StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
                return;
            }

            // Why: the typed body projects its own public fields, so this endpoint names no concrete
            // option and Fdw.Web.Api carries no reference to any option package (FDW-624).
            var summary = new ExternalIdentityProviderSummaryDto
            {
                Name = composed.Name,
                ProviderType = composed.ServiceOptionType,
            };
            composed.Configuration.PopulateSummary(summary);
            summaries.Add(summary);
        }

        ExternalIdentityProviderLog.ProviderDiscoveryReturned(logger, summaries.Count);
        await Send.OkAsync(summaries, ct).ConfigureAwait(false);
    }
}
