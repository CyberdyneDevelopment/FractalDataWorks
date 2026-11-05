using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Makes a tag through the Tags service.</summary>
public abstract class CreateTagEndpointBase : CrudCreateEndpointBase<CreateTagRequest, TagResponse>
{
    private readonly ITagService _tags;

    /// <summary>Initializes a new instance of the <see cref="CreateTagEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tags">The Tags service.</param>
    protected CreateTagEndpointBase(ILogger<CreateTagEndpointBase> logger, ITagService tags)
        : base(logger)
    {
        _tags = tags;
    }

    /// <inheritdoc />
    protected override string ResourceName => "tags";

    /// <inheritdoc />
    protected override string GetResourceName(CreateTagRequest request) => request.Name;

    /// <inheritdoc />
    /// <remarks>The service refuses a name any scope of the tenant already holds, with its own code, so there is nothing to look up here.</remarks>
    protected override Task<IGenericResult<bool>> CheckExists(CreateTagRequest request, CancellationToken ct)
        => Task.FromResult(GenericResult<bool>.Success(false));

    /// <inheritdoc />
    protected override async Task<IGenericResult<TagResponse>> Create(CreateTagRequest request, CancellationToken ct)
    {
        var created = await _tags.Create(request.Name, request.OwnerKind, request.OwnerId, request.Description, ct).ConfigureAwait(false);
        return created.IsSuccess && created.Value is { } made
            ? GenericResult<TagResponse>.Success(new TagResponse
            {
                Name = made.Name,
                OwnerKind = made.OwnerKind,
                OwnerId = made.OwnerId,
                Description = request.Description,
            })
            : created.ToNewResult<TagResponse>();
    }
}
