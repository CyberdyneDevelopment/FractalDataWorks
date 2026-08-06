using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Generic base for DELETE-by-key endpoints. The key is bound from the route
/// (<c>{Key}</c>) via <see cref="ByPropertyRequest{TKey}"/>.
/// </summary>
/// <typeparam name="TResource">The resource type for identity/logging.</typeparam>
/// <typeparam name="TKey">The key type used for lookup.</typeparam>
public abstract class DeleteByPropertyEndpointBase<TResource, TKey>
    : CrudDeleteEndpoint<ByPropertyRequest<TKey>>
    where TResource : class
    where TKey : notnull
{
    /// <summary>Expression selecting the key property of <typeparamref name="TResource"/>.</summary>
    protected abstract Expression<Func<TResource, TKey>> KeySelector { get; }

    /// <summary>Default route binds <c>{Key}</c> from the URL.</summary>
    protected override string Route => $"/{ResourceName}/{{Key}}";

    /// <summary>Returns true if a resource with the given key exists.</summary>
    protected abstract Task<IGenericResult<bool>> ExistsByKey(TKey key, CancellationToken ct);

    /// <summary>Performs the delete. The CrudDeleteEndpoint pipeline already gated by ExistsByKey.</summary>
    protected abstract Task<IGenericResult> DeleteByKey(TKey key, CancellationToken ct);

    /// <inheritdoc/>
    protected override Task<IGenericResult<bool>> CheckExistsForDelete(
        ByPropertyRequest<TKey> request, CancellationToken ct)
        => ExistsByKey(request.Key, ct);

    /// <inheritdoc/>
    protected override Task<IGenericResult> Delete(
        ByPropertyRequest<TKey> request, CancellationToken ct)
        => DeleteByKey(request.Key, ct);

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(ByPropertyRequest<TKey> request)
        => request.Key.ToString() ?? string.Empty;
}
