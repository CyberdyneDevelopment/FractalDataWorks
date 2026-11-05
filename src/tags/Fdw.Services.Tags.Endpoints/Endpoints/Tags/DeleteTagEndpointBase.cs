using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Deletes a tag and, first, every link to it.</summary>
public abstract class DeleteTagEndpointBase : CrudDeleteEndpointBase<TagNameRequest>
{
    private readonly ITagService _tags;
    private readonly ITagConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="DeleteTagEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tags">The Tags service.</param>
    /// <param name="provider">Reads the tags, to tell a missing tag from a failed delete.</param>
    protected DeleteTagEndpointBase(ILogger<DeleteTagEndpointBase> logger, ITagService tags, ITagConfigurationProvider provider)
        : base(logger)
    {
        _tags = tags;
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "tags";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(TagNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(TagNameRequest request, CancellationToken ct)
    {
        var tag = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(tag.IsSuccess);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> Delete(TagNameRequest request, CancellationToken ct)
        => _tags.Delete(request.Name, ct);
}
