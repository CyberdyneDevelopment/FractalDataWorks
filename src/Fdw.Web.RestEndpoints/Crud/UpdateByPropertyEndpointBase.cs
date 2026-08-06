using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Generic base for PUT-by-key endpoints. The key is taken from the route (<c>{Key}</c>)
/// and the update payload from the request body, exposed as <see cref="UpdateByPropertyRequest{TKey,TBody}"/>.
/// </summary>
/// <typeparam name="TBody">The update payload type (mutable fields only).</typeparam>
/// <typeparam name="TResource">The resource detail type returned after update.</typeparam>
/// <typeparam name="TKey">The key type used for lookup.</typeparam>
public abstract class UpdateByPropertyEndpointBase<TBody, TResource, TKey>
    : CrudUpdateEndpoint<UpdateByPropertyRequest<TKey, TBody>, TResource>
    where TBody : class
    where TResource : class
    where TKey : notnull
{
    /// <summary>Expression selecting the key property of <typeparamref name="TResource"/>.</summary>
    protected abstract Expression<Func<TResource, TKey>> KeySelector { get; }

    /// <summary>Default route binds <c>{Key}</c> from the URL.</summary>
    protected override string Route => $"/{ResourceName}/{{Key}}";

    /// <summary>Find existing resource by key (null inside result => 404).</summary>
    protected abstract Task<IGenericResult<TResource?>> FindByKey(TKey key, CancellationToken ct);

    /// <summary>Apply the update payload to the existing resource and persist.</summary>
    protected abstract Task<IGenericResult<TResource>> UpdateByKey(
        TKey key, TBody body, TResource existing, CancellationToken ct);

    /// <inheritdoc/>
    protected override Task<IGenericResult<TResource?>> FindForUpdate(
        UpdateByPropertyRequest<TKey, TBody> request, CancellationToken ct)
        => FindByKey(request.Key, ct);

    /// <inheritdoc/>
    protected override Task<IGenericResult<TResource>> Update(
        UpdateByPropertyRequest<TKey, TBody> request, TResource existing, CancellationToken ct)
        => UpdateByKey(request.Key, request.Body, existing, ct);

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(UpdateByPropertyRequest<TKey, TBody> request)
        => request.Key.ToString() ?? string.Empty;
}
