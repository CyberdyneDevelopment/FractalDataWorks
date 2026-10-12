using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for reading one container's fields, for a data store view that opens containers one
/// at a time instead of reading the whole store (FDW-951).
/// </summary>
public abstract class GetDataStoreContainerFieldsEndpointBase
    : CrudGetEndpointBase<DataStoreContainerRequest, List<DataStoreFieldResponse>>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <summary>Initializes a new instance of the <see cref="GetDataStoreContainerFieldsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads the container's fields.</param>
    protected GetDataStoreContainerFieldsEndpointBase(
        ILogger<GetDataStoreContainerFieldsEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider ?? throw new ArgumentNullException(nameof(dataStoreProvider));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/containers/{{ContainerId}}/fields";

    /// <summary>Returns the store and container as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataStoreContainerRequest request) => $"{request.Name}/{request.ContainerId}";

    /// <summary>Reads the container's fields, marking which its own keys list.</summary>
    /// <remarks>A store that does not exist fails as not found; a container with no fields answers an empty list.</remarks>
    protected override async Task<IGenericResult<List<DataStoreFieldResponse>?>> FindByIdentifier(
        DataStoreContainerRequest request, CancellationToken ct)
    {
        var fields = await ContainerFields.Get(_dataStoreProvider, request.Name, request.ContainerId, ct).ConfigureAwait(false);
        return fields.IsFailure
            ? fields.ToNewResult<List<DataStoreFieldResponse>?>()
            : GenericResult<List<DataStoreFieldResponse>?>.Success(fields.Value);
    }

    /// <summary>
    /// Sends the container's fields whole, or one page of them when the caller asks with <c>?paged=true</c> (FDW-1194).
    /// </summary>
    /// <param name="found">The container's fields.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the send.</returns>
    /// <remarks>
    /// A page is cut in memory from the fields the read already returned. <c>?q=</c> is compared with a field's name and its
    /// description, and <c>?total=true</c> adds how many fields were selected in all. Without <c>paged</c> the bare list is
    /// sent as it always was.
    /// </remarks>
    protected override async Task SendFound(List<DataStoreFieldResponse> found, CancellationToken ct)
    {
        var answered = await SendPageWhenAsked(found, (field, search) => search.Selects(field.Name, field.Description), ct)
            .ConfigureAwait(false);

        if (!answered)
            await base.SendFound(found, ct).ConfigureAwait(false);
    }
}
