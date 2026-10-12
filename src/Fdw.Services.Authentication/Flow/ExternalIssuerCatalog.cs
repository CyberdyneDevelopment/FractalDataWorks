using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Reads the issuers this deployment can resolve from the steps that federate.
/// </summary>
/// <remarks>
/// The step collection IS the registry: an option that federates says which authority it hands the
/// caller to, so the set is whatever this deployment has actually configured rather than a second
/// list somebody maintains beside it.
/// </remarks>
public sealed class ExternalIssuerCatalog : IExternalIssuerCatalog
{
    private readonly ILogger<ExternalIssuerCatalog> _logger;

    /// <summary>Initializes a new instance of the <see cref="ExternalIssuerCatalog"/> class.</summary>
    /// <param name="logger">Records an authority that cannot state its issuer.</param>
    public ExternalIssuerCatalog(ILogger<ExternalIssuerCatalog>? logger = null)
    {
        _logger = logger ?? NullLogger<ExternalIssuerCatalog>.Instance;
    }

    /// <inheritdoc />
    /// <remarks>
    /// An authority that cannot state its issuer is logged and leaves the set incomplete. Failing
    /// the whole read instead would let one misconfigured authority block bindings for every other
    /// one — and an unreadable authority cannot make a readable issuer untrue. The incompleteness
    /// travels with the set so that a caller about to REFUSE something knows it cannot.
    /// </remarks>
    public async Task<IGenericResult<ExternalIssuerSet>> Read(CancellationToken cancellationToken = default)
    {
        var issuers = new List<string>();
        var complete = true;

        foreach (var option in AuthenticationStepTypes.All().Values)
        {
            if (option is not IFederatingAuthenticationStep federating)
                continue;

            var issuer = await federating.GetIssuer(cancellationToken).ConfigureAwait(false);
            if (issuer.IsFailure || string.IsNullOrWhiteSpace(issuer.Value))
            {
                ExternalIssuerCatalogLog.IssuerUnreadable(
                    _logger, option.Name, issuer.CurrentMessage ?? "no issuer returned");
                complete = false;
                continue;
            }

            issuers.Add(issuer.Value);
        }

        return GenericResult<ExternalIssuerSet>.Success(new ExternalIssuerSet(issuers, complete));
    }
}
