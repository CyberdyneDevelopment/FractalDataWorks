using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Generic base for GET-by-key endpoints. Subclasses declare which property of the
/// resource is the key (via <see cref="KeySelector"/>) and implement <see cref="FindByKey"/>;
/// the route and request DTO are uniform: <c>GET /{ResourceName}/{Key}</c> with
/// <see cref="ByPropertyRequest{TKey}"/>.
/// </summary>
/// <remarks>
/// Why: replaces the proliferating <c>*ByNameEndpointBase</c>, <c>*ByIdEndpointBase</c>,
/// <c>*ByCodeEndpointBase</c> family with one parameterised abstraction. Adding a new
/// lookup key type for a resource is one new endpoint class, not one new base class.
/// </remarks>
/// <typeparam name="TResource">The resource detail type returned by the endpoint.</typeparam>
/// <typeparam name="TKey">The key type used for lookup (string, Guid, int, etc.).</typeparam>
public abstract class GetByPropertyEndpointBase<TResource, TKey>
    : CrudGetEndpointBase<ByPropertyRequest<TKey>, TResource>
    where TResource : class
    where TKey : notnull
{
    /// <summary>
    /// Gets the expression selecting the key property of <typeparamref name="TResource"/>.
    /// Used by ETag infrastructure and logging for stable identity.
    /// </summary>
    protected abstract Expression<Func<TResource, TKey>> KeySelector { get; }

    /// <summary>Default route binds the route param <c>{Key}</c> to the request's <see cref="ByPropertyRequest{TKey}.Key"/>.</summary>
    protected override string Route => $"/{ResourceName}/{{Key}}";

    /// <summary>Subclasses supply the lookup; returns null inside the result for 404.</summary>
    protected abstract Task<IGenericResult<TResource?>> FindByKey(TKey key, CancellationToken ct);

    /// <inheritdoc/>
    protected override Task<IGenericResult<TResource?>> FindByIdentifier(
        ByPropertyRequest<TKey> request, CancellationToken ct)
        => FindByKey(request.Key, ct);

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(ByPropertyRequest<TKey> request)
        => request.Key.ToString() ?? string.Empty;
}
