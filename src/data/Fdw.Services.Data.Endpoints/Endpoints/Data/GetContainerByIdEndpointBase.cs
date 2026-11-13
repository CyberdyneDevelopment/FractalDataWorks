using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for retrieving a specific container by its identifier.
/// </summary>
/// <remarks>
/// With only the container's id there is no store to start from, so every store is read down to its
/// containers to find it, and then only that container's fields (FDW-951). No other container's
/// fields are read.
/// </remarks>
public abstract class GetContainerByIdEndpointBase : CrudGetEndpointBase<ContainerIdRequest, DataStoreContainerDetailDto>
{
    // A store's paths are one level under it and their containers two.
    private const int ThroughContainers = 2;

    private readonly IDataStoreConfigurationProvider _dataStoreProvider;
    private readonly IConnectionConfigurationProvider _connectionProvider;

    /// <summary>Initializes a new instance of the <see cref="GetContainerByIdEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads the stores and their containers.</param>
    /// <param name="connectionProvider">Reads the connection of the container's data store, which its paging is laid over (FDW-5562).</param>
    protected GetContainerByIdEndpointBase(
        ILogger<GetContainerByIdEndpointBase> logger, IDataStoreConfigurationProvider dataStoreProvider, IConnectionConfigurationProvider connectionProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
        _connectionProvider = connectionProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastore-containers";

    /// <summary>Returns the container identifier.</summary>
    protected override string GetResourceIdentifier(ContainerIdRequest request) => request.Id.ToString();

    /// <summary>Finds a container by its identifier, with its fields; one that does not exist answers 404.</summary>
    protected override async Task<IGenericResult<DataStoreContainerDetailDto?>> FindByIdentifier(ContainerIdRequest request, CancellationToken ct)
    {
        var stores = await _dataStoreProvider.Get(ThroughContainers, ct).ConfigureAwait(false);
        if (stores.IsFailure)
        {
            return stores.ToNewResult<DataStoreContainerDetailDto?>();
        }

        var found = stores.Value!
            .SelectMany(store => store.Paths.SelectMany(path => path.Containers.Select(container => (store, container))))
            .FirstOrDefault(pair => pair.container.Id == request.Id);
        if (found.container is null)
        {
            return GenericResult<DataStoreContainerDetailDto?>.Success(null);
        }

        var fields = await ContainerFields.Get(_dataStoreProvider, found.store.Name, found.container.Id, ct).ConfigureAwait(false);
        if (fields.IsFailure)
            return fields.ToNewResult<DataStoreContainerDetailDto?>();

        var paging = await ContainerPagingBuilder.Detail(_dataStoreProvider, _connectionProvider, found.store, found.container, Logger, ct).ConfigureAwait(false);
        if (paging.IsFailure)
            return paging.ToNewResult<DataStoreContainerDetailDto?>();

        var detail = MapToDetail(found.container, fields.Value!);
        detail.Paging = paging.Value;
        return GenericResult<DataStoreContainerDetailDto?>.Success(detail);
    }

    /// <summary>Maps a container configuration and its fields to a detail DTO.</summary>
    /// <param name="container">The container, read as a record.</param>
    /// <param name="fields">The container's fields.</param>
    /// <returns>The detail DTO.</returns>
    protected virtual DataStoreContainerDetailDto MapToDetail(DataContainerConfiguration container, IList<DataStoreFieldResponse> fields)
    {
        return new DataStoreContainerDetailDto
        {
            Id = container.Id,
            Name = container.Name,
            ContainerType = container.TypeId ?? string.Empty,
            Description = container.Description,
            ClassificationId = container.ClassificationId,
            Format = container.Format,
            HasHeader = container.HasHeader,
            Separator = container.Separator,
            SheetName = container.SheetName,
            ContinueOnError = container.ContinueOnError,
            PropertyNameCaseInsensitive = container.PropertyNameCaseInsensitive,
            SkipValidation = container.SkipValidation,
            UseElementContent = container.UseElementContent,
            IncludeAttributes = container.IncludeAttributes,
            RootElementName = container.RootElementName,
            RowElementName = container.RowElementName,
            OmitXmlDeclaration = container.OmitXmlDeclaration,
            ContainsQuotedFields = container.ContainsQuotedFields,
            Trim = container.Trim,
            QuoteChar = container.QuoteChar,
            WriteByteOrderMark = container.WriteByteOrderMark,
            NeutralizeFormulas = container.NeutralizeFormulas,
            WriteEndpoint = container.WriteEndpoint,
            WriteMethod = container.WriteMethod,
            WriteContentType = container.WriteContentType,
            HasPagingOverride = !string.IsNullOrWhiteSpace(container.PagingStyle),
            PagingStyle = container.PagingStyle,
            Fields = fields
        };
    }
}
