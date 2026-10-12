using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads one container's fields, and the keys that say which of them are key fields, without reading
/// the rest of its data store (FDW-951).
/// </summary>
internal static class ContainerFields
{
    /// <summary>Reads a container's fields in ordinal order.</summary>
    /// <param name="provider">The data store provider.</param>
    /// <param name="dataStoreName">The data store the container is in.</param>
    /// <param name="containerId">The container's durable id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The fields, or a structured failure.</returns>
    /// <remarks>
    /// A field is a key field when a key of the container's own lists it. A foreign key lists fields
    /// too, but it names another container's key: its fields refer to a key, they are not one.
    /// </remarks>
    internal static async Task<IGenericResult<List<DataStoreFieldResponse>>> Get(
        DataStoreConfigurationProvider provider, string dataStoreName, Guid containerId, CancellationToken ct)
    {
        var fields = await provider.Get<DataContainerConfiguration, DataContainerFieldConfiguration>(
            dataStoreName, containerId, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (fields.IsFailure) return fields.ToNewResult<List<DataStoreFieldResponse>>();

        var keys = await provider.Get<DataContainerConfiguration, DataContainerKeyConfiguration>(
            dataStoreName, containerId, ConfigurationLevels.Children, ct).ConfigureAwait(false);
        if (keys.IsFailure) return keys.ToNewResult<List<DataStoreFieldResponse>>();

        var keyFieldIds = keys.Value!
            .Where(k => k.ReferencedContainerName is null)
            .SelectMany(k => k.KeyFields)
            .Select(kf => kf.DataContainerFieldId)
            .ToHashSet();

        return GenericResult<List<DataStoreFieldResponse>>.Success(fields.Value!
            .OrderBy(f => f.Ordinal)
            .Select(f => new DataStoreFieldResponse
            {
                Id = f.Id,
                Name = f.Name,
                NativeDataType = f.DataType,
                FrameworkDataType = f.FrameworkDataType,
                IsNullable = f.IsNullable,
                IsKey = keyFieldIds.Contains(f.Id),
                Ordinal = f.Ordinal,
                Description = f.Description,
                ClassificationId = f.ClassificationId,
                DatumTypeId = f.DatumTypeId,
                MaxLength = f.MaxLength,
                Precision = f.Precision,
                Scale = f.Scale
            })
            .ToList());
    }
}
