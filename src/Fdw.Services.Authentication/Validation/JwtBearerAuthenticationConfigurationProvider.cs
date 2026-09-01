using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Commands;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Reads the JwtBearer rows of <c>auth.AuthenticationService</c>.
/// </summary>
/// <remarks>
/// Closes the base over the domain's implementation contract, because that is what the domain
/// provider's registry accepts — <c>Register&lt;T&gt;</c> constrains <c>T</c> to
/// <see cref="IImplementationConfigurationProvider{T}"/> of the domain contract, so a provider closed
/// over anything narrower cannot be registered.
/// <para>
/// It also satisfies <see cref="IJwtBearerAuthenticationConfigurationProvider"/>, which closes over
/// this option's own contract so a caller that needs an audience gets one back rather than the domain
/// contract it would have to test. The two closings are different types and the interface is
/// invariant, so those members are implemented explicitly. Reads narrow what the base returns; a
/// write of some other kind's configuration fails loud rather than being coerced.
/// </para>
/// </remarks>
public sealed class JwtBearerAuthenticationConfigurationProvider
    : ImplementationConfigurationProvider<
          IAuthenticationServiceImplementationConfiguration,
          JwtBearerAuthenticationConfiguration,
          JwtBearerAuthenticationConfigurationCommand>,
      IJwtBearerAuthenticationConfigurationProvider
{
    private readonly ILogger _log;

    /// <summary>Initializes a new instance of the <see cref="JwtBearerAuthenticationConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for provider operations.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the store these rows live on.</param>
    /// <param name="dataStoreName">The store the host declared these rows on.</param>
    /// <param name="pathName">The path the rows live under.</param>
    public JwtBearerAuthenticationConfigurationProvider(
        ILogger<JwtBearerAuthenticationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "auth")
        : base(logger ?? NullLogger<JwtBearerAuthenticationConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
        _log = logger ?? NullLogger<JwtBearerAuthenticationConfigurationProvider>.Instance;
    }

    async Task<IGenericResult<IJwtBearerAuthenticationConfiguration>>
        IImplementationConfigurationProvider<IJwtBearerAuthenticationConfiguration>.Get(
            Guid domainId, CancellationToken cancellationToken)
        => Narrow(await Get(domainId, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IReadOnlyList<IJwtBearerAuthenticationConfiguration>>>
        IImplementationConfigurationProvider<IJwtBearerAuthenticationConfiguration>.Get(
            CancellationToken cancellationToken)
    {
        var all = await Get(cancellationToken).ConfigureAwait(false);
        if (!all.IsSuccess || all.Value is null)
            return all.ToNewResult<IReadOnlyList<IJwtBearerAuthenticationConfiguration>>();

        // Every row this provider reads is a JwtBearer row, so a member of another kind means the
        // registry dispatched the wrong provider — a defect, not a row to skip.
        var narrowed = new List<IJwtBearerAuthenticationConfiguration>(all.Value.Count);
        foreach (var member in all.Value)
        {
            if (member is not IJwtBearerAuthenticationConfiguration typed)
                return GenericResult<IReadOnlyList<IJwtBearerAuthenticationConfiguration>>.Failure(
                    AuthenticationValidationLog.ImplementationKindMismatch(
                        _log, nameof(JwtBearerAuthenticationConfiguration), member.GetType().Name));
            narrowed.Add(typed);
        }

        return GenericResult<IReadOnlyList<IJwtBearerAuthenticationConfiguration>>.Success(narrowed);
    }

    async Task<IGenericResult<IJwtBearerAuthenticationConfiguration>>
        IImplementationConfigurationProvider<IJwtBearerAuthenticationConfiguration>.Save(
            IJwtBearerAuthenticationConfiguration record, CancellationToken cancellationToken)
        => record is JwtBearerAuthenticationConfiguration typed
            ? Narrow(await Save(typed, cancellationToken).ConfigureAwait(false))
            : GenericResult<IJwtBearerAuthenticationConfiguration>.Failure(
                AuthenticationValidationLog.ImplementationKindMismatch(
                    _log, nameof(JwtBearerAuthenticationConfiguration), record?.GetType().Name ?? "(null)"));

    Task<IGenericResult> IImplementationConfigurationProvider<IJwtBearerAuthenticationConfiguration>.Delete(
        Guid domainId, CancellationToken cancellationToken)
        => Delete(domainId, cancellationToken);

    private IGenericResult<IJwtBearerAuthenticationConfiguration> Narrow(
        IGenericResult<IAuthenticationServiceImplementationConfiguration> result)
    {
        if (!result.IsSuccess || result.Value is null)
            return result.ToNewResult<IJwtBearerAuthenticationConfiguration>();

        return result.Value is IJwtBearerAuthenticationConfiguration typed
            ? GenericResult<IJwtBearerAuthenticationConfiguration>.Success(typed)
            : GenericResult<IJwtBearerAuthenticationConfiguration>.Failure(
                AuthenticationValidationLog.ImplementationKindMismatch(
                    _log, nameof(JwtBearerAuthenticationConfiguration), result.Value.GetType().Name));
    }
}
