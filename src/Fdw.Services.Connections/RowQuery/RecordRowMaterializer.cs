using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// Materializes already-filtered/joined rows (flat name→value dictionaries) into a requested POCO type
/// <c>T</c> — a single row, or a collection — through the generated
/// <c>PocoMapperCollection.ByName(typeof(T).Name)</c> mapper via <see cref="RecordDictionaryReader"/>
/// (coercing, so it must call <c>MapFromReader</c>, never <c>MapFromDictionary</c>).
/// </summary>
public static class RecordRowMaterializer
{
    /// <summary>
    /// Materializes <paramref name="rows"/> into <typeparamref name="T"/>: a single element when
    /// <typeparamref name="T"/> is not a recognised collection shape, otherwise a collection of the
    /// element type. A no-mapper condition and a per-row mapping failure both fail loud with
    /// structured MessageLogging — never a silent default/empty materialization.
    /// </summary>
    public static IGenericResult<T> Materialize<T>(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IStorageContainer container,
        ILogger logger)
    {
        var targetType = typeof(T);
        return IsCollectionType(targetType, out var itemType)
            ? MaterializeCollection<T>(rows, itemType!, container, logger)
            : MaterializeSingle<T>(rows, targetType, container, logger);
    }

    private static IGenericResult<T> MaterializeCollection<T>(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, Type itemType, IStorageContainer container, ILogger logger)
    {
        var mapper = PocoMapperCollection.ByName(itemType.Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<T>.Failure(RecordQueryLog.NoMapperFound(logger, itemType.Name));

        var list = mapper.CreateList();
        using var reader = new RecordDictionaryReader(rows);
        while (reader.Read())
        {
            var mapResult = mapper.MapFromReader(reader, container);
            if (!mapResult.IsSuccess)
                return GenericResult<T>.Failure(RecordQueryLog.MaterializationFailed(logger, itemType.Name, mapResult.CurrentMessage));

            list.Add(mapResult.Value);
        }

        return GenericResult<T>.Success((T)ConvertToCollectionType(list, typeof(T), itemType));
    }

    private static IGenericResult<T> MaterializeSingle<T>(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, Type targetType, IStorageContainer container, ILogger logger)
    {
        if (rows.Count == 0)
            return GenericResult<T>.Success(default!);

        var mapper = PocoMapperCollection.ByName(targetType.Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<T>.Failure(RecordQueryLog.NoMapperFound(logger, targetType.Name));

        using var reader = new RecordDictionaryReader(rows);
        reader.Read();
        var mapResult = mapper.MapFromReader(reader, container);
        if (!mapResult.IsSuccess)
            return GenericResult<T>.Failure(RecordQueryLog.MaterializationFailed(logger, targetType.Name, mapResult.CurrentMessage));

        return GenericResult<T>.Success((T)mapResult.Value!);
    }

    private static bool IsCollectionType(Type type, out Type? itemType)
    {
        if (type.IsArray)
        {
            itemType = type.GetElementType();
            return true;
        }

        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            if (genericTypeDef == typeof(IEnumerable<>) || genericTypeDef == typeof(List<>)
                || genericTypeDef == typeof(IList<>) || genericTypeDef == typeof(ICollection<>))
            {
                itemType = type.GetGenericArguments()[0];
                return true;
            }
        }

        var enumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface != null)
        {
            itemType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        itemType = null;
        return false;
    }

    private static object ConvertToCollectionType(IList list, Type targetType, Type itemType)
    {
        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(itemType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }
        return list;
    }
}
