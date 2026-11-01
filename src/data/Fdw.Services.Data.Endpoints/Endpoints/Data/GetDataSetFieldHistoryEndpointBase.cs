using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Reads every stored version of a data set field (API-174).</summary>
/// <remarks>
/// Field configuration is versioned on write, so the history already exists as retired rows of
/// data.DataSetField under the field's logical Id; this reads them through
/// <see cref="ConfigurationVersions"/>. Each version lists the properties that differ from the one
/// before it, so a screen does not have to diff them itself.
/// </remarks>
public abstract class GetDataSetFieldHistoryEndpointBase : Endpoint<DataSetFieldRequest, DataSetFieldHistoryResponse>
{
    private const string FieldContainer = "DataSetField";

    private readonly DataSetConfigurationProvider _dataSets;
    private readonly IConfigurationGatewayProvider _configurationGateways;
    private readonly ILogger<GetDataSetFieldHistoryEndpointBase> _logger;

    /// <inheritdoc />
    /// <remarks>
    /// The rows are configuration rows in the data set store the host declared, so they go through that store's
    /// configuration gateway: the main gateway refuses a request over a host-declared store.
    /// </remarks>
    protected GetDataSetFieldHistoryEndpointBase(
        DataSetConfigurationProvider dataSets,
        IConfigurationGatewayProvider configurationGateways,
        ILogger<GetDataSetFieldHistoryEndpointBase>? logger = null)
    {
        _dataSets = dataSets;
        _configurationGateways = configurationGateways;
        _logger = logger ?? NullLogger<GetDataSetFieldHistoryEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/datasets/{Name}/fields/{Field}/history");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get a data set field's history";
            s.Description = "Returns every stored version of the field, oldest first, each with the properties that changed from the version before it.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DataSetFieldRequest req, CancellationToken ct)
    {
        DataSetFieldHistoryEndpointLog.ReadingHistory(_logger, req.Name, req.Field);

        var resolved = await DataSetFieldResolver.Resolve(_dataSets, req.Name, req.Field, ct).ConfigureAwait(false);
        if (resolved.IsFailure) { await SendError(resolved, ct).ConfigureAwait(false); return; }
        if (resolved.Value is not { } found)
        {
            DataSetFieldHistoryEndpointLog.FieldNotFound(_logger, req.Name, req.Field);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var gateway = _configurationGateways.Get(_dataSets.DataStoreName);
        if (gateway.IsFailure) { await SendError(gateway, ct).ConfigureAwait(false); return; }

        var versions = await ConfigurationVersions.Read<DataSetFieldConfiguration>(
            gateway.Value!,
            new DataStoreTarget(_dataSets.DataStoreName, _dataSets.PathName, FieldContainer),
            found.Field.Id,
            v => v.ModifyDate,
            ct).ConfigureAwait(false);
        if (versions.IsFailure)
        {
            DataSetFieldHistoryEndpointLog.HistoryReadFailed(_logger, req.Name, req.Field);
            await SendError(versions, ct).ConfigureAwait(false);
            return;
        }

        var dtos = new List<DataSetFieldVersionDto>(versions.Value!.Count);
        DataSetFieldConfiguration? previous = null;
        foreach (var version in versions.Value)
        {
            dtos.Add(ToDto(dtos.Count + 1, version, previous));
            previous = version;
        }

        DataSetFieldHistoryEndpointLog.HistoryRead(_logger, req.Name, req.Field, dtos.Count);
        await Send.OkAsync(
            new DataSetFieldHistoryResponse { FieldId = found.Field.Id, Field = found.Field.Name, DataSet = req.Name, Versions = dtos },
            ct).ConfigureAwait(false);
    }

    private static DataSetFieldVersionDto ToDto(int number, DataSetFieldConfiguration v, DataSetFieldConfiguration? previous) => new()
    {
        Version = number,
        IsCurrent = v.IsCurrent,
        IsDeleted = v.IsDeleted,
        ChangedAt = v.ModifyDate,
        ChangedBy = v.ModifyBy,
        ChangedOnBehalfOf = v.ModifyOnBehalfOf,
        Changes = previous is null ? [] : Changed(previous, v),
        Name = v.Name,
        TypeName = v.TypeName,
        Description = v.Description,
        Role = v.Role,
        IsKey = v.IsKey,
        IsNullable = v.IsNullable,
        IsRequired = v.IsRequired,
        IsIndexed = v.IsIndexed,
        MaxLength = v.MaxLength,
        DefaultValue = v.DefaultValue,
        Ordinal = v.Ordinal,
        CalculationName = v.CalculationName,
    };

    // The properties a person edits. Audit and key columns change on every version by definition,
    // so they would make every version look changed.
    private static List<string> Changed(DataSetFieldConfiguration a, DataSetFieldConfiguration b)
    {
        var changes = new List<string>();
        if (!string.Equals(a.Name, b.Name, StringComparison.Ordinal)) changes.Add(nameof(b.Name));
        if (!string.Equals(a.TypeName, b.TypeName, StringComparison.Ordinal)) changes.Add(nameof(b.TypeName));
        if (!string.Equals(a.Description, b.Description, StringComparison.Ordinal)) changes.Add(nameof(b.Description));
        if (!string.Equals(a.Role, b.Role, StringComparison.Ordinal)) changes.Add(nameof(b.Role));
        if (a.IsKey != b.IsKey) changes.Add(nameof(b.IsKey));
        if (a.IsNullable != b.IsNullable) changes.Add(nameof(b.IsNullable));
        if (a.IsRequired != b.IsRequired) changes.Add(nameof(b.IsRequired));
        if (a.IsIndexed != b.IsIndexed) changes.Add(nameof(b.IsIndexed));
        if (a.MaxLength != b.MaxLength) changes.Add(nameof(b.MaxLength));
        if (!string.Equals(a.DefaultValue, b.DefaultValue, StringComparison.Ordinal)) changes.Add(nameof(b.DefaultValue));
        if (a.Ordinal != b.Ordinal) changes.Add(nameof(b.Ordinal));
        if (!string.Equals(a.CalculationName, b.CalculationName, StringComparison.Ordinal)) changes.Add(nameof(b.CalculationName));
        if (a.IsDeleted != b.IsDeleted) changes.Add(nameof(b.IsDeleted));
        return changes;
    }

    private async Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}
