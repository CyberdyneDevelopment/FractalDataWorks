using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// The catalog read for one request, shared by every context the request runs statements in, so a
/// procedure's statements see the same catalog as the batch that called it and it is read only once.
/// </summary>
public sealed class CatalogCache
{
    private readonly TdsEndpointBase _endpoint;
    private IGenericResult<CatalogSnapshot>? _read;

    /// <summary>Initializes a new instance of the <see cref="CatalogCache"/> class.</summary>
    /// <param name="endpoint">The endpoint the catalog is read from, as the caller.</param>
    public CatalogCache(TdsEndpointBase endpoint)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    /// <summary>Gets the catalog once it has been read; null before.</summary>
    public CatalogSnapshot? Loaded
    {
        get
        {
            if (_read is null || _read.IsFailure)
                return null;

            return _read.Value;
        }
    }

    /// <summary>Reads the catalog the first time it is asked for, and answers the same read after.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The catalog, or the failure reading it.</returns>
    public async Task<IGenericResult<CatalogSnapshot>> Load(CancellationToken cancellationToken = default)
    {
        if (_read is null)
            _read = await CatalogSnapshot.Load(_endpoint, cancellationToken).ConfigureAwait(false);

        return _read;
    }
}
