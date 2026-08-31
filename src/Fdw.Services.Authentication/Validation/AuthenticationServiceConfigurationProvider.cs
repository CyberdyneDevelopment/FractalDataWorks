using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Commands;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Reads the authentication services a host trusts, and dispatches each to the provider for its kind.
/// </summary>
/// <remarks>
/// The domain half of the pair. It holds the gateway onto the store the rows live on and a registry
/// of implementation providers keyed by <c>ServiceOptionType</c>; an option registers itself into
/// that registry during Initialize, when both providers can be resolved.
/// <para>
/// A caller asks by name or by id. This reads the domain row, takes the kind it names, and hands the
/// row's id to the provider for that kind. What comes back is the implementation configuration.
/// </para>
/// </remarks>
public class AuthenticationServiceConfigurationProvider
    : ServiceConfigurationProviderBase<
          AuthenticationServiceConfiguration,
          IAuthenticationServiceImplementationConfiguration,
          AuthenticationServiceConfigurationCommand>,
      IAuthenticationServiceConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for provider operations.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the store these rows live on.</param>
    /// <param name="dataStoreName">The store the host declared these rows on.</param>
    /// <param name="pathName">The path the rows live under.</param>
    public AuthenticationServiceConfigurationProvider(
        ILogger<AuthenticationServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "auth")
        : base(logger ?? NullLogger<AuthenticationServiceConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }

    /// <summary>Reads the domain row for a named authentication service, without dispatching.</summary>
    /// <param name="name">The declared service name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// The scheme selector needs the issuer and the kind, both of which are on the domain row; it
    /// does not need what the kind uses to check a signature.
    /// </remarks>
    public Task<IGenericResult<AuthenticationServiceConfiguration>> GetHeader(
        string name,
        CancellationToken cancellationToken = default)
        => GetHeaderByName(name, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IAuthenticationServiceConfiguration>>> GetHeaders(
        CancellationToken cancellationToken = default)
    {
        var rows = await Get(cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is not null
            ? GenericResult<IReadOnlyList<IAuthenticationServiceConfiguration>>.Success(rows.Value)
            : rows.ToNewResult<IReadOnlyList<IAuthenticationServiceConfiguration>>();
    }

    /// <inheritdoc />
    protected override AuthenticationServiceConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
