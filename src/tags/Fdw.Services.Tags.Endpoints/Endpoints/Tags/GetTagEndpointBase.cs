using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Reads one tag by name.</summary>
public abstract class GetTagEndpointBase : CrudGetEndpointBase<TagNameRequest, TagResponse>
{
    private readonly ITagConfigurationProvider _tags;

    /// <summary>Initializes a new instance of the <see cref="GetTagEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tags">Reads the tags.</param>
    protected GetTagEndpointBase(ILogger<GetTagEndpointBase> logger, ITagConfigurationProvider tags)
        : base(logger)
    {
        _tags = tags;
    }

    /// <inheritdoc />
    protected override string ResourceName => "tags";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(TagNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<TagResponse?>> FindByIdentifier(TagNameRequest request, CancellationToken ct)
    {
        var tag = await _tags.Get(request.Name, ct).ConfigureAwait(false);
        return tag.IsSuccess && tag.Value is { } found
            ? GenericResult<TagResponse?>.Success(TagResponseMapper.From(found))
            : GenericResult<TagResponse?>.Success(null);
    }
}
