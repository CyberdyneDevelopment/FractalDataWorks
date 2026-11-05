using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Tags.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Tags;

/// <summary>
/// The owners rule on a host that does not run the tenant, organization and workspace services (the ETL host and the scheduler): it refuses to list,
/// rather than failing the construction of the endpoint (FDW-4100).
/// </summary>
public sealed class UnavailableTagOwners : ITagOwners
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UnavailableTagOwners"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public UnavailableTagOwners(ILogger<UnavailableTagOwners>? logger = null)
    {
        _logger = logger ?? NullLogger<UnavailableTagOwners>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<TagOwner>>> List(CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<IReadOnlyList<TagOwner>>>(
            GenericResult<IReadOnlyList<TagOwner>>.Failure(TagsResultCodes.ByName("TagOwnersUnavailable"), _logger, TagDetails.Of()));
}
