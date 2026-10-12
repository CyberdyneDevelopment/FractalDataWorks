using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Builds a container's field and key rows from a request, the same way wherever a container is
/// supplied: adding one to a path, or creating a store with its whole tree in one call (FDW-920).
/// </summary>
public static class DataStoreContainerBuilder
{
    private const string ForeignKeyType = "Foreign";

    /// <summary>Builds the container's field rows, pre-assigning each an Id.</summary>
    /// <param name="fields">The fields the caller supplied.</param>
    /// <returns>The field rows.</returns>
    /// <remarks>
    /// A key can only name a field the same request defines, and it does so by field name -- the
    /// field has no Id yet anywhere the caller could have read one. Assigning it here, before the
    /// keys are built, is what lets a key resolve that name to a real Id in the same call.
    /// </remarks>
    public static List<DataContainerFieldConfiguration> Fields(IEnumerable<CreateDataStoreContainerFieldRequest> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        return fields.Select(f => new DataContainerFieldConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = f.Name,
            DataType = f.DataType,
            FrameworkDataType = f.FrameworkDataType,
            IsNullable = f.IsNullable,
            Ordinal = f.Ordinal,
            IsSystemProvided = f.IsSystemProvided,
            DatumTypeId = f.DatumTypeId,
            Description = f.Description,
            ClassificationId = f.ClassificationId,
            MaxLength = f.MaxLength,
            Precision = f.Precision,
            Scale = f.Scale,
        }).ToList();
    }

    /// <summary>Builds the container's keys, resolving each key field and Foreign reference by name.</summary>
    /// <param name="dataStoreName">The store the container belongs to, for the log.</param>
    /// <param name="keys">The keys the caller supplied.</param>
    /// <param name="container">The container, with its fields already built.</param>
    /// <param name="referenceable">The containers a Foreign key may reference, with their keys built.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The key rows, or the first key that could not be resolved.</returns>
    /// <remarks>
    /// Names, not Ids: the caller cannot know a field's Id before the same request creates it, and a
    /// Foreign key's referenced container and key are identified the way every other request in this
    /// endpoint family identifies them -- by name.
    /// </remarks>
    public static IGenericResult<List<DataContainerKeyConfiguration>> Keys(
        string dataStoreName,
        IEnumerable<CreateDataStoreContainerKeyRequest> keys,
        DataContainerConfiguration container,
        IEnumerable<DataContainerConfiguration> referenceable,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(referenceable);

        var candidates = referenceable.ToList();
        var built = new List<DataContainerKeyConfiguration>();
        foreach (var keyRequest in keys)
        {
            var key = Key(dataStoreName, keyRequest, container, candidates, logger);
            if (key.IsFailure || key.Value is not { } resolved)
                return key.ToNewResult<List<DataContainerKeyConfiguration>>();

            built.Add(resolved);
        }

        return GenericResult<List<DataContainerKeyConfiguration>>.Success(built);
    }

    /// <summary>Whether a key references another container's key.</summary>
    /// <param name="key">The key the caller supplied.</param>
    /// <returns><see langword="true"/> for a Foreign key.</returns>
    public static bool IsForeign(CreateDataStoreContainerKeyRequest key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return string.Equals(key.TypeId, ForeignKeyType, StringComparison.OrdinalIgnoreCase);
    }

    private static IGenericResult<DataContainerKeyConfiguration> Key(
        string dataStoreName,
        CreateDataStoreContainerKeyRequest keyRequest,
        DataContainerConfiguration container,
        List<DataContainerConfiguration> candidates,
        ILogger logger)
    {
        if (KeyTypes.ByName(keyRequest.TypeId) == KeyTypes.NotFound)
        {
            return GenericResult<DataContainerKeyConfiguration>.Failure(
                DataStoreEndpointLog.InvalidKeyType(logger, keyRequest.Name, keyRequest.TypeId));
        }

        var reference = ReferencedKeyId(dataStoreName, keyRequest, candidates, logger);
        if (reference.IsFailure)
            return reference.ToNewResult<DataContainerKeyConfiguration>();

        var keyFields = KeyFields(keyRequest, container, logger);
        if (keyFields.IsFailure || keyFields.Value is not { } fields)
            return keyFields.ToNewResult<DataContainerKeyConfiguration>();

        return GenericResult<DataContainerKeyConfiguration>.Success(new DataContainerKeyConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = keyRequest.Name,
            TypeId = keyRequest.TypeId,
            Description = keyRequest.Description,
            ReferencedKeyId = reference.Value,
            KeyFields = fields,
        });
    }

    private static IGenericResult<Guid?> ReferencedKeyId(
        string dataStoreName,
        CreateDataStoreContainerKeyRequest keyRequest,
        List<DataContainerConfiguration> candidates,
        ILogger logger)
    {
        if (!IsForeign(keyRequest))
            return GenericResult<Guid?>.Success(null);

        if (string.IsNullOrEmpty(keyRequest.ReferencedContainerName) || string.IsNullOrEmpty(keyRequest.ReferencedKeyName))
            return GenericResult<Guid?>.Failure(DataStoreEndpointLog.ForeignKeyReferenceMissing(logger, keyRequest.Name));

        var referencedContainer = candidates.FirstOrDefault(
            c => string.Equals(c.Name, keyRequest.ReferencedContainerName, StringComparison.OrdinalIgnoreCase));
        if (referencedContainer is null)
        {
            return GenericResult<Guid?>.Failure(DataStoreEndpointLog.ReferencedContainerNotFound(
                logger, keyRequest.Name, keyRequest.ReferencedContainerName, dataStoreName));
        }

        var referencedKey = referencedContainer.Keys.FirstOrDefault(
            k => string.Equals(k.Name, keyRequest.ReferencedKeyName, StringComparison.OrdinalIgnoreCase));
        if (referencedKey is null)
        {
            return GenericResult<Guid?>.Failure(DataStoreEndpointLog.ReferencedKeyNotFound(
                logger, keyRequest.Name, keyRequest.ReferencedKeyName, keyRequest.ReferencedContainerName));
        }

        return GenericResult<Guid?>.Success(referencedKey.Id);
    }

    private static IGenericResult<List<DataContainerKeyFieldConfiguration>> KeyFields(
        CreateDataStoreContainerKeyRequest keyRequest,
        DataContainerConfiguration container,
        ILogger logger)
    {
        var keyFields = new List<DataContainerKeyFieldConfiguration>();
        foreach (var keyField in keyRequest.KeyFields)
        {
            var field = container.Fields.FirstOrDefault(
                f => string.Equals(f.Name, keyField.FieldName, StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                return GenericResult<List<DataContainerKeyFieldConfiguration>>.Failure(
                    DataStoreEndpointLog.KeyFieldNotFoundInContainer(logger, keyRequest.Name, keyField.FieldName));
            }

            keyFields.Add(new DataContainerKeyFieldConfiguration
            {
                Id = Guid.CreateVersion7(),
                DataContainerFieldId = field.Id,
                Ordinal = keyField.Ordinal,
                KeyType = keyRequest.TypeId,
            });
        }

        return GenericResult<List<DataContainerKeyFieldConfiguration>>.Success(keyFields);
    }
}
