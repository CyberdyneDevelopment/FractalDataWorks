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
/// Reads the LocalKey rows of <c>auth.AuthenticationService</c>.
/// </summary>
/// <remarks>
/// Closes the base over the domain's implementation contract, because that is what the domain
/// provider's registry accepts — <c>Register&lt;T&gt;</c> constrains <c>T</c> to
/// <see cref="IImplementationConfigurationProvider{T}"/> of the domain contract, so a provider closed
/// over anything narrower cannot be registered.
/// <para>
/// It also satisfies <see cref="ILocalKeyAuthenticationConfigurationProvider"/>, which closes over
/// this option's own contract so a caller that needs an audience gets one back rather than the domain
/// contract it would have to test. The two closings are different types and the interface is
/// invariant, so those members are implemented explicitly. Reads narrow what the base returns; a
/// write of some other kind's configuration fails loud rather than being coerced.
/// </para>
/// </remarks>
public sealed class LocalKeyAuthenticationConfigurationProvider
    : ImplementationConfigurationProvider<
          IAuthenticationServiceImplementationConfiguration,
          LocalKeyAuthenticationConfiguration,
          LocalKeyAuthenticationConfigurationCommand>,
      ILocalKeyAuthenticationConfigurationProvider
{
    private readonly ILogger _log;

    /// <summary>Initializes a new instance of the <see cref="LocalKeyAuthenticationConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for provider operations.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the store these rows live on.</param>
    /// <param name="dataStoreName">The store the host declared these rows on.</param>
    /// <param name="pathName">The path the rows live under.</param>
    public LocalKeyAuthenticationConfigurationProvider(
        ILogger<LocalKeyAuthenticationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "auth")
        : base(logger ?? NullLogger<LocalKeyAuthenticationConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
        _log = logger ?? NullLogger<LocalKeyAuthenticationConfigurationProvider>.Instance;
    }

    async Task<IGenericResult<ILocalKeyAuthenticationConfiguration>>
        IImplementationConfigurationProvider<ILocalKeyAuthenticationConfiguration>.Get(
            Guid domainId, CancellationToken cancellationToken)
        => Narrow(await Get(domainId, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IReadOnlyList<ILocalKeyAuthenticationConfiguration>>>
        IImplementationConfigurationProvider<ILocalKeyAuthenticationConfiguration>.Get(
            CancellationToken cancellationToken)
    {
        var all = await Get(cancellationToken).ConfigureAwait(false);
        if (!all.IsSuccess || all.Value is null)
            return all.ToNewResult<IReadOnlyList<ILocalKeyAuthenticationConfiguration>>();

        // Every row this provider reads is a LocalKey row, so a member of another kind means the
        // registry dispatched the wrong provider — a defect, not a row to skip.
        var narrowed = new List<ILocalKeyAuthenticationConfiguration>(all.Value.Count);
        foreach (var member in all.Value)
        {
            if (member is not ILocalKeyAuthenticationConfiguration typed)
                return GenericResult<IReadOnlyList<ILocalKeyAuthenticationConfiguration>>.Failure(
                    AuthenticationValidationLog.ImplementationKindMismatch(
                        _log, nameof(LocalKeyAuthenticationConfiguration), member.GetType().Name));
            narrowed.Add(typed);
        }

        return GenericResult<IReadOnlyList<ILocalKeyAuthenticationConfiguration>>.Success(narrowed);
    }

    async Task<IGenericResult<ILocalKeyAuthenticationConfiguration>>
        IImplementationConfigurationProvider<ILocalKeyAuthenticationConfiguration>.Save(
            ILocalKeyAuthenticationConfiguration record, CancellationToken cancellationToken)
        => record is LocalKeyAuthenticationConfiguration typed
            ? Narrow(await Save(typed, cancellationToken).ConfigureAwait(false))
            : GenericResult<ILocalKeyAuthenticationConfiguration>.Failure(
                AuthenticationValidationLog.ImplementationKindMismatch(
                    _log, nameof(LocalKeyAuthenticationConfiguration), record?.GetType().Name ?? "(null)"));

    Task<IGenericResult> IImplementationConfigurationProvider<ILocalKeyAuthenticationConfiguration>.Delete(
        Guid domainId, CancellationToken cancellationToken)
        => Delete(domainId, cancellationToken);

    private IGenericResult<ILocalKeyAuthenticationConfiguration> Narrow(
        IGenericResult<IAuthenticationServiceImplementationConfiguration> result)
    {
        if (!result.IsSuccess || result.Value is null)
            return result.ToNewResult<ILocalKeyAuthenticationConfiguration>();

        return result.Value is ILocalKeyAuthenticationConfiguration typed
            ? GenericResult<ILocalKeyAuthenticationConfiguration>.Success(typed)
            : GenericResult<ILocalKeyAuthenticationConfiguration>.Failure(
                AuthenticationValidationLog.ImplementationKindMismatch(
                    _log, nameof(LocalKeyAuthenticationConfiguration), result.Value.GetType().Name));
    }
}
