using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Lists the tags the caller can see.</summary>
public abstract class ListTagsEndpointBase : CrudListEndpointBase<TagResponse>
{
    private readonly ITagConfigurationProvider _tags;

    /// <summary>Initializes a new instance of the <see cref="ListTagsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tags">Reads the tags.</param>
    protected ListTagsEndpointBase(ILogger<ListTagsEndpointBase> logger, ITagConfigurationProvider tags)
        : base(logger)
    {
        _tags = tags;
    }

    /// <inheritdoc />
    protected override string ResourceName => "tags";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<TagResponse>>> LoadItems(CancellationToken ct)
    {
        var tags = await _tags.Get(ct).ConfigureAwait(false);
        return tags.IsSuccess && tags.Value is { } all
            ? GenericResult<List<TagResponse>>.Success([.. all.Select(TagResponseMapper.From).OrderBy(tag => tag.Name, System.StringComparer.OrdinalIgnoreCase)])
            : tags.ToNewResult<List<TagResponse>>();
    }
}
