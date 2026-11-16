using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataOptionRows;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Changes one option row (FDW-5664): <c>PATCH /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options/{Id}</c>
/// with any of <c>option</c>, <c>active</c> and <c>value</c>; what the body leaves out stays as it is.
/// </summary>
public abstract class ChangeDataOptionRowEndpointBase : CrudUpdateEndpointBase<ChangeDataOptionRowRequest, DataOptionRowPayload>
{
    private readonly IDataOptionRows _rows;

    /// <summary>Initializes a new instance of the <see cref="ChangeDataOptionRowEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="rows">Reads and changes the option rows.</param>
    protected ChangeDataOptionRowEndpointBase(ILogger<ChangeDataOptionRowEndpointBase> logger, IDataOptionRows rows) : base(logger)
    {
        _rows = rows;
    }

    /// <summary>Gets the resource name used for the documentation tag.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Gets the write policy: option rows change what a load of the data store imports.</summary>
    protected override string WritePolicy => "datastores:write";

    /// <summary>Gets the nested-resource route.</summary>
    protected override string Route => "/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options/{Id}";

    /// <summary>Gets the summary shown for the route.</summary>
    protected override string EndpointSummary => "Change an option row";

    /// <summary>Gets the description shown for the route.</summary>
    protected override string EndpointDescription => "Changes the option (Include or Exclude for SelectList), whether the row is active, or its value; fields left out stay.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(ChangeDataOptionRowRequest request) => $"{request.Name}/{request.PathName}/{request.ContainerName}/options/{request.Id}";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRowPayload?>> FindForUpdate(ChangeDataOptionRowRequest request, CancellationToken ct)
    {
        var found = await _rows.Get(Key(request), ct).ConfigureAwait(false);
        return found.IsFailure
            ? found.ToNewResult<DataOptionRowPayload?>()
            : GenericResult<DataOptionRowPayload?>.Success(found.Value is { } row ? DataOptionRowMapping.ToPayload(row) : null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRowPayload>> Update(ChangeDataOptionRowRequest request, DataOptionRowPayload existing, CancellationToken ct)
    {
        var changed = await _rows.Change(new DataOptionRowChange { Key = Key(request), Option = request.Option, Active = request.Active, Value = request.Value }, ct).ConfigureAwait(false);
        return changed.IsFailure || changed.Value is null
            ? changed.ToNewResult<DataOptionRowPayload>()
            : GenericResult<DataOptionRowPayload>.Success(DataOptionRowMapping.ToPayload(changed.Value));
    }

    private static DataOptionRowKey Key(ChangeDataOptionRowRequest request)
        => new() { Target = DataOptionRowMapping.Target(request.Name, request.PathName, request.ContainerName), Id = request.Id };
}
