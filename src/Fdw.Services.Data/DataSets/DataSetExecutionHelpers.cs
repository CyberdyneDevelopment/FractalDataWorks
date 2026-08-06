using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;

namespace Fdw.Services.Data;

/// <summary>
/// Shared row-level execution helpers for dataset type strategies (Simple, Compound).
/// Centralizes physical→logical field rename, calculated field application, and dictionary-to-type
/// conversion so no strategy duplicates them.
/// </summary>
// Why: extracted from SimpleDataSetType so CompoundDataSetType can apply the same post-query
// transforms (calculated fields) without code duplication. The helpers are internal so they remain
// invisible outside the Fdw.Services.Data assembly.
internal static class DataSetExecutionHelpers
{
    /// <summary>
    /// Returns the container name for a source: ContainerName → HttpEndpoint → FilePath → null.
    /// Null means no container is configured; the caller must fail loud.
    /// </summary>
    internal static string? GetContainerName(DataSetSourceConfiguration source)
    {
        if (!string.IsNullOrEmpty(source.ContainerName))
            return source.ContainerName;
        if (!string.IsNullOrEmpty(source.HttpEndpoint))
            return source.HttpEndpoint;
        if (!string.IsNullOrEmpty(source.FilePath))
            return source.FilePath;
        return null;
    }

    /// <summary>
    /// Applies physical→logical field renames using the supplied mapping (key=logical, value=physical)
    /// inverted at call time. Returns a new result with renamed rows; unmapped physical keys pass
    /// through unchanged. Only operates on row-enumerable T values.
    /// </summary>
    internal static IGenericResult<T> ApplyFieldRename<T>(
        DataSetExecutionContext ctx,
        IGenericResult<T> sourceResult,
        IReadOnlyDictionary<string, string> fieldMappings,
        string sourceName)
    {
        if (!sourceResult.IsSuccess || sourceResult.Value == null)
            return sourceResult;

        // Why: string is IEnumerable<char> so exclude it before the IEnumerable check; any other
        // non-enumerable passes through unchanged — rename applies only to row collections.
        if (sourceResult.Value is string)
            return sourceResult;
        if (sourceResult.Value is not System.Collections.IEnumerable enumerable)
            return sourceResult;

        var physicalToLogical = new Dictionary<string, string>(fieldMappings.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in fieldMappings)
            physicalToLogical[kvp.Value] = kvp.Key;

        DataGatewayLogger.ApplyingFieldRename(ctx.Logger, physicalToLogical.Count, sourceName);

        var renamedRows = new List<Dictionary<string, object?>>();
        foreach (var item in enumerable)
        {
            if (item == null) continue;
            var dictResult = ObjectToDictionary(item);
            if (!dictResult.IsSuccess) continue;

            var original = dictResult.Value!;
            var renamed = new Dictionary<string, object?>(original.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in original)
            {
                // Why: an unmapped physical key passes through with its original name (not a fallback —
                // unmapped columns are intentionally kept verbatim).
                var logicalKey = physicalToLogical.TryGetValue(kvp.Key, out var lk) ? lk : kvp.Key;
                renamed[logicalKey] = kvp.Value;
            }
            renamedRows.Add(renamed);
        }

        return ConvertDictionariesToType<T>(ctx, renamedRows);
    }

    /// <summary>
    /// Applies calculated fields (expressions/calculators defined on DataFieldConfiguration) to each
    /// row in the result. Only operates on row-enumerable T values; non-collection types return a
    /// failure result.
    /// </summary>
    internal static IGenericResult<T> ApplyCalculatedFields<T>(
        DataSetExecutionContext ctx,
        IGenericResult<T> sourceResult,
        List<DataFieldConfiguration> calculatedFields,
        string dataSetName)
    {
        if (!sourceResult.IsSuccess || sourceResult.Value == null)
            return sourceResult;

        if (sourceResult.Value is not System.Collections.IEnumerable enumerable)
            return GenericResult<T>.Failure(DataGatewayLogger.CalculatedResultConversionFailed(ctx.Logger, typeof(T).Name));

        var enrichedResults = ProcessRowsWithCalculatedFields(ctx, enumerable, calculatedFields, dataSetName, out var rowCount);
        if (enrichedResults == null)
            return GenericResult<T>.Failure(DataServiceResultCodes.ByName("ObjectConversionFailed"));

        DataGatewayLogger.CalculatedFieldsApplied(ctx.Logger, rowCount, 0);
        return ConvertDictionariesToType<T>(ctx, enrichedResults);
    }

    internal static List<Dictionary<string, object?>>? ProcessRowsWithCalculatedFields(
        DataSetExecutionContext ctx,
        System.Collections.IEnumerable enumerable,
        List<DataFieldConfiguration> calculatedFields,
        string dataSetName,
        out int rowCount)
    {
        var enrichedResults = new List<Dictionary<string, object?>>();
        rowCount = 0;

        foreach (var item in enumerable)
        {
            if (item == null) continue;

            var dictResult = ObjectToDictionary(item);
            if (!dictResult.IsSuccess)
                return null;

            var dict = dictResult.Value!;
            var schema = CreateSchemaFromDictionary(dict);
            var row = DataRow.FromDictionary(schema, dict);

            foreach (var calcField in calculatedFields)
            {
                if (calcField.Calculator == null) continue;
                try
                {
                    dict[calcField.Name] = calcField.Calculator(row);
                }
                catch (Exception ex)
                {
                    DataGatewayLogger.CalculatedFieldFailed(ctx.Logger, calcField.Name, dataSetName, ex.Message);
                }
            }

            enrichedResults.Add(dict);
            rowCount++;
        }

        return enrichedResults;
    }

    // Why: dictionary/ExpandoObject rows copy directly; a POCO projects through its generated mapper
    // (column-keyed). An unmapped POCO fails loud — no reflection fallback.
    internal static IGenericResult<Dictionary<string, object?>> ObjectToDictionary(object obj)
    {
        if (obj is Dictionary<string, object?> dict)
            return GenericResult<Dictionary<string, object?>>.Success(
                new Dictionary<string, object?>(dict, StringComparer.OrdinalIgnoreCase));

        if (obj is ExpandoObject expando)
            return GenericResult<Dictionary<string, object?>>.Success(
                new Dictionary<string, object?>((IDictionary<string, object?>)expando, StringComparer.OrdinalIgnoreCase));

        var mapper = PocoMapperCollection.ByName(obj.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<Dictionary<string, object?>>.Failure(
                DataServiceResultCodes.ByName("MapperNotFound"),
                ResultDetails.Create("TypeName", obj.GetType().Name));

        return GenericResult<Dictionary<string, object?>>.Success(
            new Dictionary<string, object?>(mapper.MapToParameters(obj), StringComparer.OrdinalIgnoreCase));
    }

    internal static DataSchema CreateSchemaFromDictionary(Dictionary<string, object?> dict)
    {
        var fields = dict.Select((kvp, index) => new SchemaField(
            kvp.Key,
            kvp.Value?.GetType() ?? typeof(object),
            index)).ToList();

        return DataSchema.FromFields(fields);
    }

    internal static IGenericResult<T> ConvertDictionariesToType<T>(DataSetExecutionContext ctx, List<Dictionary<string, object?>> dictionaries)
    {
        var targetType = typeof(T);

        if (!targetType.IsGenericType)
            return GenericResult<T>.Failure(DataGatewayLogger.CalculatedResultConversionFailed(ctx.Logger, targetType.Name));

        var genericDef = targetType.GetGenericTypeDefinition();
        if (genericDef != typeof(IEnumerable<>) && genericDef != typeof(List<>) && genericDef != typeof(ICollection<>))
            return GenericResult<T>.Failure(DataGatewayLogger.CalculatedResultConversionFailed(ctx.Logger, targetType.Name));

        var itemType = targetType.GetGenericArguments()[0];

        if (itemType == typeof(Dictionary<string, object?>) || itemType == typeof(IDictionary<string, object?>))
            return GenericResult<T>.Success((T)(object)dictionaries);

        if (itemType == typeof(object))
            // Why: dictionary rows ARE object rows — IEnumerable<Dictionary<string,object?>> covariantly
            // satisfies IEnumerable<object>, and serializes identically to JSON. No ExpandoObject/DLR.
            return GenericResult<T>.Success((T)(object)dictionaries.AsEnumerable());

        return ConvertToPocos<T>(dictionaries, itemType);
    }

    internal static IGenericResult<T> ConvertToPocos<T>(List<Dictionary<string, object?>> dictionaries, Type itemType)
    {
        var mapper = PocoMapperCollection.ByName(itemType.Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<T>.Failure(
                DataServiceResultCodes.ByName("MapperNotFound"),
                ResultDetails.Create("TypeName", itemType.Name));

        var list = mapper.CreateList();
        foreach (var dict in dictionaries)
        {
            var itemResult = mapper.MapFromDictionary(dict);
            if (!itemResult.IsSuccess)
                return GenericResult<T>.Failure(
                    DataServiceResultCodes.ByName("PocoMappingFailed"),
                    ResultDetails.Create("TypeName", itemType.Name, "Reason", itemResult.CurrentMessage ?? "Unknown error"));
            list.Add(itemResult.Value!);
        }

        return GenericResult<T>.Success((T)list);
    }
}
