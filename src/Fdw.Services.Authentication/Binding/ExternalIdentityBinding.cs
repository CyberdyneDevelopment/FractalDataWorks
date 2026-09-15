using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Binding;

/// <summary>
/// Resolves an external subject to a local principal by reading <c>auth.ExternalIdentity</c>.
/// </summary>
/// <remarks>
/// Matches on the pair and nothing else. There is no email path, no display-name path and no
/// fall-through to a fuzzy match — an authenticated stranger with no row is unbound, which is a
/// decision for provisioning policy rather than something to guess at.
/// </remarks>
public sealed class ExternalIdentityBinding : IPrincipalBinding
{
    private readonly IExternalIdentityConfigurationProvider _identities;
    private readonly ITenantResolver _tenants;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ILogger<ExternalIdentityBinding> _logger;

    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityBinding"/> class.</summary>
    /// <param name="identities">Reads the binding rows.</param>
    /// <param name="tenants">Supplies the tenant a user belongs to.</param>
    /// <param name="authContextAccessor">Establishes the trusted context for pre-authentication reads.</param>
    /// <param name="logger">The logger.</param>
    public ExternalIdentityBinding(
        IExternalIdentityConfigurationProvider identities,
        ITenantResolver tenants,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<ExternalIdentityBinding>? logger = null)
    {
        _identities = identities ?? throw new ArgumentNullException(nameof(identities));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _logger = logger ?? NullLogger<ExternalIdentityBinding>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Principal?>> Resolve(
        string issuer, string subjectId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subjectId))
            return GenericResult<Principal?>.Failure(BindingLog.LookupIncomplete(_logger));

        // This platform authentication lookup precedes the request authentication context.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var all = await _identities.Get(cancellationToken).ConfigureAwait(false);
        if (all.IsFailure)
            return all.ToNewResult<Principal?>();

        var matches = (all.Value ?? [])
            .Where(i => i.IsActive
                && string.Equals(i.Provider, issuer, StringComparison.Ordinal)
                && string.Equals(i.ExternalSubject, subjectId, StringComparison.Ordinal))
            .ToList();

        if (matches.Count == 0)
        {
            BindingLog.Unbound(_logger, issuer);
            return GenericResult<Principal?>.Success(null);
        }

        if (matches.Count > 1)
        {
            return GenericResult<Principal?>.Failure(
                BindingLog.Ambiguous(_logger, issuer, matches.Count));
        }

        var tenant = await _tenants.TenantFor(matches[0].UserId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
            return tenant.ToNewResult<Principal?>();

        BindingLog.Bound(_logger, issuer, matches[0].UserId);

        return GenericResult<Principal?>.Success(
            new Principal { Id = matches[0].UserId, TenantId = tenant.Value });
    }
}
