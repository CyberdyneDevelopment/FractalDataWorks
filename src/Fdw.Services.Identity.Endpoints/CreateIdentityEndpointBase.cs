using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// Creates a managed identity backed by <typeparamref name="TConfig"/>.
/// </summary>
/// <typeparam name="TConfig">The typed body for the mechanism this endpoint creates.</typeparam>
/// <typeparam name="TRequest">The request carrying that mechanism's own fields.</typeparam>
/// <remarks>
/// One derived endpoint per mechanism, each on its own route, because the fields differ: client
/// credentials needs a client id and token endpoint, JWT federation needs an assertion source and
/// location. A single route taking a union of both would accept combinations that cannot work.
/// </remarks>
public abstract class CreateIdentityEndpointBase<TConfig, TRequest>
    : CrudCreateEndpointBase<TRequest, IdentityDetailResponse>
    where TConfig : class, IIdentityServiceImplementationConfiguration
    where TRequest : CreateIdentityRequest, new()
{
    /// <summary>Gets the provider that reads and writes identity configuration.</summary>
    protected abstract IServiceConfigurationProvider<IdentityServiceConfiguration> Identities { get; }

    /// <inheritdoc />
    protected override string ResourceName => "identities";

    /// <inheritdoc />
    protected override string WritePolicy => "identities:write";

    /// <inheritdoc />
    protected override string GetResourceName(TRequest request) => request?.Name ?? string.Empty;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(TRequest request, CancellationToken ct)
    {
        var existing = await Identities.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existing.IsSuccess && existing.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<IdentityDetailResponse>> Create(
        TRequest request,
        CancellationToken ct)
    {
        if (request.SecretManagerName is not { Length: > 0 })
        {
            return GenericResult<IdentityDetailResponse>.Failure(
                IdentityEndpointLog.CreateRequestIncomplete(Logger, request.Name, nameof(request.SecretManagerName)));
        }

        if (request.SecretKeyName is not { Length: > 0 })
        {
            // NO FALLBACKS: without a key there is nothing to resolve the credential by, and an
            // identity that cannot acquire a token is a row that fails at first use rather than here.
            return GenericResult<IdentityDetailResponse>.Failure(
                IdentityEndpointLog.CreateRequestIncomplete(Logger, request.Name, nameof(request.SecretKeyName)));
        }

        var identityId = Guid.CreateVersion7();
        var typedBody = CreateTypedBody(request, identityId);

        var identity = new IdentityServiceConfiguration
        {
            Id = identityId,
            Name = request.Name,
            ServiceType = "Identity",
            SectionName = "Identities",
            ServiceOptionType = request.ServiceOptionType,
            Description = request.Description,
            Configuration = typedBody,
        };

        // One save for the whole aggregate: the provider writes the header, then dispatches on
        // ServiceOptionType so the registered typed provider writes the body.
        var saved = await Identities.Save(identity, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<IdentityDetailResponse>();
        }

        IdentityEndpointLog.IdentityCreated(Logger, request.Name, request.ServiceOptionType);
        return GenericResult<IdentityDetailResponse>.Success(MapToDetail(identity, typedBody));
    }

    /// <summary>Builds the mechanism's typed body from the request.</summary>
    /// <param name="request">The create request.</param>
    /// <param name="identityId">The id minted for the header, which the body joins on.</param>
    /// <returns>The typed body to persist beneath the header.</returns>
    protected abstract TConfig CreateTypedBody(TRequest request, Guid identityId);

    /// <summary>Maps the saved aggregate to the response.</summary>
    /// <param name="identity">The header that was written.</param>
    /// <param name="typedBody">The body that was written beneath it.</param>
    /// <returns>The detail returned to the caller.</returns>
    protected abstract IdentityDetailResponse MapToDetail(
        IdentityServiceConfiguration identity,
        TConfig typedBody);
}
