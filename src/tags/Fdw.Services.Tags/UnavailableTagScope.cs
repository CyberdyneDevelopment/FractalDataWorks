using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Tags.Configuration;
using Fdw.Services.Tags.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Tags;

/// <summary>
/// The scope rule of a host that registers no workspace provider (FDW-3816): it refuses to put a tag on a thing, with a code that says why.
/// </summary>
/// <remarks>
/// The ETL host and the scheduler run the Tags service so their lists can answer <c>?tag=</c>, which reads the links and needs no scope. Putting a
/// tag on a thing does need the rule, and the workspace provider it reads is registered with the workspace endpoints, which only the API maps. The
/// service is still built on those hosts, so this stands in for the rule and fails each write loud, never allowing a tag nobody checked.
/// </remarks>
public sealed class UnavailableTagScope : ITagScope
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UnavailableTagScope"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public UnavailableTagScope(ILogger<UnavailableTagScope>? logger = null)
    {
        _logger = logger ?? NullLogger<UnavailableTagScope>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult> Allows(TagDomainConfiguration tag, ITaggableKind kind, Guid thingId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IGenericResult>(GenericResult<bool>.Failure(
            TagsResultCodes.ByName("TagScopeUnavailable"), _logger,
            TagDetails.Of("name", tag.Name, "kind", kind.Name, "thingId", thingId.ToString("D"))));
    }
}
