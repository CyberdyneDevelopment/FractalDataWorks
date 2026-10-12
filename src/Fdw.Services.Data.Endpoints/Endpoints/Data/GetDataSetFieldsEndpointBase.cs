using Fdw.Services.Data.Clients.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for retrieving fields for a specific data set.
/// Delegates reads to DataSetConfigurationProvider (which assembles field hierarchy on Get).
/// </summary>
public abstract class GetDataSetFieldsEndpointBase : CrudGetEndpointBase<DataSetNameRequest, List<DataSetFieldPayload>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected GetDataSetFieldsEndpointBase(ILogger<GetDataSetFieldsEndpointBase> logger, DataSetConfigurationProvider dataSetProvider) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <summary>
    /// Routes at <c>/datasets/{Name}/fields</c>. The default CRUD route is <c>/datasets/{Name}</c>,
    /// which collides with the GetDataSet endpoint and shadows this one. The fields sub-resource
    /// segment makes the route non-colliding and matches the calculation client's call.
    /// </summary>
    protected override string Route => $"/{ResourceName}/{{Name}}/fields";

    /// <summary>Returns the data set name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataSetNameRequest request) => request.Name;

    /// <summary>Finds a data set by name and returns its fields.</summary>
    protected override Task<IGenericResult<List<DataSetFieldPayload>?>> FindByIdentifier(DataSetNameRequest request, CancellationToken ct)
    {
        return LoadDataSetFields(request.Name, ct);
    }

    /// <summary>
    /// Loads the data set's fields and returns them as field DTOs. Override for custom behavior.
    /// </summary>
    /// <remarks>
    /// Reads the data set as a record, for its Id, then its fields as records: its sources, joins and
    /// filters are not composed to answer a question about its fields (FDW-951).
    /// </remarks>
    protected virtual async Task<IGenericResult<List<DataSetFieldPayload>?>> LoadDataSetFields(string dataSetName, CancellationToken ct)
    {
        DataSetEndpointLog.LoadingFields(Logger, dataSetName);

        var result = await _dataSetProvider.Get(dataSetName, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<List<DataSetFieldPayload>?>();

        if (result.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, dataSetName);
            return GenericResult<List<DataSetFieldPayload>?>.Success((List<DataSetFieldPayload>?)null);
        }

        var read = await _dataSetProvider.Get<DataSetFieldConfiguration>(dataSetName, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (read.IsFailure) return read.ToNewResult<List<DataSetFieldPayload>?>();

        var fields = read.Value!
            .OrderBy(f => f.Ordinal)
            .Select(f => DataSetQueryHelper.MapToFieldDto(result.Value.Id, f))
            .ToList();

        return GenericResult<List<DataSetFieldPayload>?>.Success((List<DataSetFieldPayload>?)fields);
    }

    /// <summary>
    /// Sends the fields whole, or one page of them when the caller asks with <c>?paged=true</c> (FDW-1194).
    /// </summary>
    /// <param name="found">The data set's fields, in ordinal order.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the send.</returns>
    /// <remarks>
    /// A page is cut in memory from the fields the read already returned, so it is the same read; what changes is how
    /// much the caller holds. <c>?q=</c> is compared with a field's name and its description, <c>?dataType=</c> keeps the
    /// fields of one data type (without regard to case), and <c>?total=true</c> adds how many fields were selected in all.
    /// Without <c>paged</c> the bare list is sent as it always was.
    /// </remarks>
    protected override async Task SendFound(List<DataSetFieldPayload> found, CancellationToken ct)
    {
        var dataType = HttpContext.Request.Query.TryGetValue("dataType", out var asked) && !string.IsNullOrWhiteSpace(asked.ToString())
            ? asked.ToString()
            : null;

        var answered = await SendPageWhenAsked(
            found,
            (field, search) => search.Selects(field.Name, field.Description)
                && (dataType is null || string.Equals(field.DataType, dataType, StringComparison.OrdinalIgnoreCase)),
            ct).ConfigureAwait(false);

        if (!answered)
            await base.SendFound(found, ct).ConfigureAwait(false);
    }
}
