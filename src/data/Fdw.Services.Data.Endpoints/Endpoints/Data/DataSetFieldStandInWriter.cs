using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Persists a field's stand-in: a fresh parent row, then the matching strategy's own child row.</summary>
/// <remarks>
/// Each set mints a brand new parent Id (not a new version of the prior one) since the strategy
/// itself, not just its parameters, can change. UX_DataSetFieldStandIn_Field_Current enforces one
/// current row per field, but ConfigurationSaveCommand's own version-on-write only retires by
/// matching <c>Id</c> — a fresh Id never matches, so the prior current row has to be retired
/// explicitly first (same shape as DetachWorkspaceRelationshipEndpointBase's retire), or the insert
/// below violates that index. Every child row always points at its own write's freshly-minted
/// parent Id, so no old child row is ever still reachable through the new parent — no explicit
/// child delete is needed.
/// </remarks>
internal static class DataSetFieldStandInWriter
{
    public static async Task<IGenericResult<DataSetFieldStandInResponse>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        Guid dataSetFieldId, string fieldName, SetDataSetFieldStandInRequest request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var standInTarget = new DataStoreTarget(dataStoreName, pathName, "DataSetFieldStandIn");

        var retired = await RetireCurrent(gateway, standInTarget, dataSetFieldId, now, markDeleted: false, ct).ConfigureAwait(false);
        if (retired.IsFailure) return retired.ToNewResult<DataSetFieldStandInResponse>();

        var standIn = new DataSetFieldStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = $"{fieldName}:{request.Strategy}",
            DataSetFieldId = dataSetFieldId,
            NullPercent = request.NullPercent,
            Seed = request.Seed,
            CreateDate = now,
        };

        var standInResult = await gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<DataSetFieldStandInConfiguration>(standIn, replacesCurrent: true), standInTarget, ct).ConfigureAwait(false);
        if (standInResult.IsFailure) return standInResult.ToNewResult<DataSetFieldStandInResponse>();

        var childResult = await WriteChild(gateway, dataStoreName, pathName, standIn, request, now, ct).ConfigureAwait(false);
        if (childResult.IsFailure) return childResult.ToNewResult<DataSetFieldStandInResponse>();

        return GenericResult<DataSetFieldStandInResponse>.Success(new DataSetFieldStandInResponse
        {
            FieldId = dataSetFieldId,
            Field = fieldName,
            Strategy = request.Strategy,
            NullPercent = standIn.NullPercent,
            Seed = standIn.Seed,
            Parameters = childResult.Value!,
        });
    }

    private static async Task<IGenericResult<Dictionary<string, object?>>> WriteChild(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken ct)
    {
        var handler = StandInHandlers.For(request.Name, request.Field, request.Strategy);
        return handler.IsFailure
            ? handler.ToNewResult<Dictionary<string, object?>>()
            : await handler.Value!.Write(gateway, dataStoreName, pathName, standIn, request, now, ct).ConfigureAwait(false);
    }

    /// <summary>Clears a field's stand-in: its current row is retired and marked deleted (FDW-982).</summary>
    /// <returns>True when a stand-in was cleared; false when the field had none.</returns>
    /// <remarks>
    /// A soft delete, like every other configuration delete. Every read starts from the current,
    /// undeleted parent row (<see cref="DataSetFieldStandInReader.FindActive"/>), so once that row is
    /// retired the field reads as having no stand-in, and its strategy row is unreachable, just as a
    /// replaced stand-in's is. The row stays as history.
    /// </remarks>
    public static Task<IGenericResult<bool>> Clear(
        IDataGateway gateway, string dataStoreName, string pathName, Guid dataSetFieldId, CancellationToken ct)
        => RetireCurrent(gateway, new DataStoreTarget(dataStoreName, pathName, "DataSetFieldStandIn"), dataSetFieldId,
            DateTimeOffset.UtcNow, markDeleted: true, ct);

    // Retires the field's current stand-in row, if it has one. A replace retires it; a clear also marks
    // it deleted. The answer is whether there was a row to retire.
    private static async Task<IGenericResult<bool>> RetireCurrent(
        IDataGateway gateway, DataStoreTarget standInTarget, Guid dataSetFieldId, DateTimeOffset now, bool markDeleted, CancellationToken ct)
    {
        var existingCommand = new QueryCommand<DataSetFieldStandInConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "DataSetFieldId", Operator = FilterOperators.ByName("Equal"), Value = dataSetFieldId },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existing = await gateway.Execute<IEnumerable<DataSetFieldStandInConfiguration>>(existingCommand, standInTarget, ct).ConfigureAwait(false);
        if (existing.IsFailure) return existing.ToNewResult<bool>();

        var current = existing.Value?.FirstOrDefault();
        if (current is null) return GenericResult<bool>.Success(false);

        current.IsCurrent = false;
        current.IsDeleted = markDeleted;
        current.ModifyDate = now;

        var updateCommand = new UpdateCommand<DataSetFieldStandInConfiguration>(current)
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = current.Id }
            }
        };

        var retired = await gateway.Execute<int>(updateCommand, standInTarget, ct).ConfigureAwait(false);
        return retired.IsFailure ? retired.ToNewResult<bool>() : GenericResult<bool>.Success(true);
    }
}
