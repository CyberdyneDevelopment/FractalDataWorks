using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Tags;

/// <summary>Lists the owners a tag can be given to (FDW-4100).</summary>
/// <remarks>
/// Why a service and not the endpoint reading the providers: the owners come from the tenant, organization and workspace services, which only
/// the API host registers. The tag endpoint group registers <see cref="TagOwners"/> where they exist and <see cref="UnavailableTagOwners"/> where
/// they do not, as it does for <see cref="ITagScope"/>, so a host that maps the tag endpoints still starts.
/// </remarks>
public interface ITagOwners
{
    /// <summary>Lists the tenant the caller acts in, then the organizations and the workspaces the tenant-scoped providers return.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The owners, or a failure when the tenant is unknown, a provider could not be read, or this host cannot list them.</returns>
    Task<IGenericResult<IReadOnlyList<TagOwner>>> List(CancellationToken cancellationToken = default);
}
