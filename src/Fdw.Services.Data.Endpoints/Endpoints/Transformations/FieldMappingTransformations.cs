using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads and writes the attachments of transformations to field mappings
/// (<c>transform.FieldMappingTransformation</c>) for the transformation endpoints.
/// </summary>
/// <remarks>
/// Child configuration rows written through <see cref="ConfigurationSaveCommand{T}"/> -- the path this
/// workspace reserves for field-mapping transform rows. One place, so the four endpoints that touch
/// attachments cannot disagree about where they live or what counts as current.
/// </remarks>
internal static class FieldMappingTransformations
{
    private const string PathName = "transform";
    private const string ContainerName = "FieldMappingTransformation";

    private static DataStoreTarget Target(string connectionName) => new(connectionName, PathName, ContainerName);

    /// <summary>The current attachments of one field mapping, in chain order.</summary>
    internal static async Task<IGenericResult<IReadOnlyList<FieldMappingTransformationConfiguration>>> ForFieldMapping(
        IDataGateway gateway, string connectionName, Guid fieldMappingId, CancellationToken ct)
    {
        var result = await gateway
            .Execute<IEnumerable<FieldMappingTransformationConfiguration>>(
                new QueryCommand<FieldMappingTransformationConfiguration>
                {
                    Filter = DataSetQueryHelper.ActiveFilterFor(
                        nameof(FieldMappingTransformationConfiguration.DataSetFieldMappingId), fieldMappingId)
                },
                Target(connectionName), ct)
            .ConfigureAwait(false);
        if (result.IsFailure)
            return result.ToNewResult<IReadOnlyList<FieldMappingTransformationConfiguration>>();

        return GenericResult<IReadOnlyList<FieldMappingTransformationConfiguration>>.Success(
            [.. result.Value!.OrderBy(a => a.Ordinal)]);
    }

    /// <summary>The current attachments of one transformation, across every field mapping.</summary>
    internal static async Task<IGenericResult<IReadOnlyList<FieldMappingTransformationConfiguration>>> ForTransformation(
        IDataGateway gateway, string connectionName, Guid transformationId, CancellationToken ct)
    {
        var result = await gateway
            .Execute<IEnumerable<FieldMappingTransformationConfiguration>>(
                new QueryCommand<FieldMappingTransformationConfiguration>
                {
                    Filter = DataSetQueryHelper.ActiveFilterFor(
                        nameof(FieldMappingTransformationConfiguration.TransformationId), transformationId)
                },
                Target(connectionName), ct)
            .ConfigureAwait(false);
        if (result.IsFailure)
            return result.ToNewResult<IReadOnlyList<FieldMappingTransformationConfiguration>>();

        return GenericResult<IReadOnlyList<FieldMappingTransformationConfiguration>>.Success([.. result.Value!]);
    }

    /// <summary>Writes an attachment (new, or a new version of an existing one by its Id).</summary>
    internal static async Task<IGenericResult> Save(
        IDataGateway gateway, string connectionName, FieldMappingTransformationConfiguration attachment, CancellationToken ct)
        => await gateway
            .Execute<ConfigurationWriteResult>(
                new ConfigurationSaveCommand<FieldMappingTransformationConfiguration>(
                    attachment, replacesCurrent: true),
                Target(connectionName), ct)
            .ConfigureAwait(false);

    /// <summary>Retires an attachment by its Id.</summary>
    internal static async Task<IGenericResult> Retire(
        IDataGateway gateway, string connectionName, Guid attachmentId, CancellationToken ct)
        => await gateway
            .Execute<ConfigurationWriteResult>(
                new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [attachmentId], markDeleted: true),
                Target(connectionName), ct)
            .ConfigureAwait(false);
}
