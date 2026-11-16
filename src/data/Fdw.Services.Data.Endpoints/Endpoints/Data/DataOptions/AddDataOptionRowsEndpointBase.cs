using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataOptionRows;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Adds several option rows to a container at once, all or none (FDW-5664):
/// <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options</c>.
/// </summary>
/// <remarks>The service checks every row and every stored pair before it writes; a refusal names the row and why.</remarks>
public abstract class AddDataOptionRowsEndpointBase : CrudCreateEndpointBase<AddDataOptionRowsRequest, DataOptionRowsPayload>
{
    private readonly IDataOptionRows _rows;

    /// <summary>Initializes a new instance of the <see cref="AddDataOptionRowsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="rows">Adds the option rows.</param>
    protected AddDataOptionRowsEndpointBase(ILogger<AddDataOptionRowsEndpointBase> logger, IDataOptionRows rows) : base(logger)
    {
        _rows = rows;
    }

    /// <summary>Gets the resource name used for the documentation tag.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Gets the write policy: option rows change what a load of the data store imports.</summary>
    protected override string WritePolicy => "datastores:write";

    /// <summary>Gets the nested-resource route.</summary>
    protected override string Route => "/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options";

    /// <summary>Gets the summary shown for the route.</summary>
    protected override string EndpointSummary => "Add option rows to a container";

    /// <summary>Gets the description shown for the route.</summary>
    protected override string EndpointDescription =>
        "Adds the rows in one write, or none of them when any is refused; the answer names the refused row and the reason.";

    /// <inheritdoc />
    protected override string GetResourceName(AddDataOptionRowsRequest request) => $"{request.Name}/{request.PathName}/{request.ContainerName}/options";

    /// <summary>Answers that nothing is in the way: the service refuses a pair that is already stored, naming it.</summary>
    protected override Task<IGenericResult<bool>> CheckExists(AddDataOptionRowsRequest request, CancellationToken ct)
        => Task.FromResult<IGenericResult<bool>>(GenericResult<bool>.Success(false));

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRowsPayload>> Create(AddDataOptionRowsRequest request, CancellationToken ct)
    {
        var added = await _rows.Add(
            new DataOptionRowsAddition
            {
                Target = DataOptionRowMapping.Target(request.Name, request.PathName, request.ContainerName),
                Rows = [.. request.Rows.Select(row => new NewDataOptionRow { Column = row.Column, Value = row.Value, OptionType = row.OptionType, Option = row.Option, Active = row.Active })],
            },
            ct).ConfigureAwait(false);

        return added.IsFailure || added.Value is null
            ? added.ToNewResult<DataOptionRowsPayload>()
            : GenericResult<DataOptionRowsPayload>.Success(new DataOptionRowsPayload { Items = [.. added.Value.Select(DataOptionRowMapping.ToPayload)] });
    }
}
