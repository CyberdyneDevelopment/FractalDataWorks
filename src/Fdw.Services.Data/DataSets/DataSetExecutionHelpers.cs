using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.Transformations;
using Fdw.Results;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Fdw.Services.Data.Transformations;

namespace Fdw.Services.Data;

/// <summary>
/// Shared row-level execution helpers for dataset type strategies (Simple, Compound).
/// Centralizes physical→logical field rename, calculated field application, and dictionary-to-type
/// conversion so no strategy duplicates them.
/// </summary>
internal static class DataSetExecutionHelpers
{
    /// <summary>Refuses a run whose configuration is not of the kind that runs it.</summary>
    /// <param name="ctx">The execution context.</param>
    /// <param name="kind">The kind that was asked to run the data set.</param>
    /// <typeparam name="T">The result type the failure is returned as.</typeparam>
    /// <returns>The failure.</returns>
    internal static IGenericResult<T> KindMismatch<T>(DataSetExecutionContext ctx, string kind)
        => GenericResult<T>.Failure(
            DataServiceResultCodes.ByName("DataSetKindMismatch"), ctx.Logger,
            ResultDetails.Create("dataSet", ctx.Config.Name, "kind", kind));

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
                var logicalKey = physicalToLogical.TryGetValue(kvp.Key, out var lk) ? lk : kvp.Key;
                renamed[logicalKey] = kvp.Value;
            }
            renamedRows.Add(renamed);
        }

        return ConvertDictionariesToType<T>(ctx, renamedRows);
    }

    /// <summary>
    /// The attached transformation chains of every field across <paramref name="sources"/>, keyed by
    /// logical field name (FDW-877). A data set's logical field names are unique across its sources.
    /// </summary>
    internal static async Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>>>> ResolveChains(
        DataSetExecutionContext ctx, IEnumerable<DataSetSourceConfiguration> sources, CancellationToken cancellationToken)
    {
        var merged = new Dictionary<string, IReadOnlyList<ResolvedFieldTransformation>>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            var chains = await ctx.FieldTransformations.ForSource(source, cancellationToken).ConfigureAwait(false);
            if (chains.IsFailure)
                return chains;

            foreach (var (field, chain) in chains.Value!)
                merged[field] = chain;
        }

        return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>>>.Success(merged);
    }

    /// <summary>
    /// Runs each field's attached transformation chain (FDW-877) over every row, in chain order, after
    /// the physical-to-logical rename. A field the read did not select is left alone. Any link that
    /// fails fails the read: returning the untransformed value would present raw data as transformed.
    /// </summary>
    internal static async Task<IGenericResult<T>> ApplyFieldTransformations<T>(
        DataSetExecutionContext ctx,
        IGenericResult<T> sourceResult,
        IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>> chains,
        string sourceName,
        CancellationToken cancellationToken)
    {
        if (!sourceResult.IsSuccess || sourceResult.Value == null || chains.Count == 0)
            return sourceResult;
        if (sourceResult.Value is string || sourceResult.Value is not System.Collections.IEnumerable enumerable)
            return sourceResult;

        var now = DateTimeOffset.UtcNow;
        var rows = new List<Dictionary<string, object?>>();
        foreach (var item in enumerable)
        {
            if (item == null) continue;
            var dictResult = ObjectToDictionary(item);
            if (!dictResult.IsSuccess)
                return dictResult.ToNewResult<T>();

            var row = dictResult.Value!;
            foreach (var (field, chain) in chains)
            {
                if (!row.TryGetValue(field, out var value))
                    continue;

                var transformed = await RunChain(ctx, field, chain, value, row, now, cancellationToken).ConfigureAwait(false);
                if (transformed.IsFailure)
                    return transformed.ToNewResult<T>();

                row[field] = transformed.Value;
            }

            rows.Add(row);
        }

        FieldMappingTransformationResolverLog.Applied(ctx.Logger, chains.Count, rows.Count, sourceName);
        return ConvertDictionariesToType<T>(ctx, rows);
    }

    /// <summary>
    /// The <see cref="DataRecord"/> form of <see cref="ApplyFieldTransformations{T}"/>, for the federated
    /// strategy, which joins sources in memory as records. A record is immutable, so each one with a
    /// transformed field is rebuilt over the same schema.
    /// </summary>
    internal static async Task<IGenericResult<List<DataRecord>>> ApplyFieldTransformations(
        DataSetExecutionContext ctx,
        List<DataRecord> records,
        IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>> chains,
        string dataSetName,
        CancellationToken cancellationToken)
    {
        if (chains.Count == 0)
            return GenericResult<List<DataRecord>>.Success(records);

        var now = DateTimeOffset.UtcNow;
        var output = new List<DataRecord>(records.Count);
        foreach (var record in records)
        {
            var values = record.Values.ToArray();
            var current = record.ToDictionary();
            foreach (var (field, chain) in chains)
            {
                var ordinal = record.Schema.GetFieldOrdinal(field);
                if (ordinal < 0)
                    continue;

                var transformed = await RunChain(ctx, field, chain, values[ordinal], current, now, cancellationToken).ConfigureAwait(false);
                if (transformed.IsFailure)
                    return transformed.ToNewResult<List<DataRecord>>();

                values[ordinal] = transformed.Value;
            }

            output.Add(new DataRecord(record.Schema, values));
        }

        FieldMappingTransformationResolverLog.Applied(ctx.Logger, chains.Count, output.Count, dataSetName);
        return GenericResult<List<DataRecord>>.Success(output);
    }

    private static async Task<IGenericResult<object?>> RunChain(
        DataSetExecutionContext ctx,
        string field,
        IReadOnlyList<ResolvedFieldTransformation> chain,
        object? value,
        IReadOnlyDictionary<string, object?> currentRecord,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var link in chain)
        {
            var transformed = await link.Operation.Transform(
                value,
                new TransformationContext
                {
                    Parameters = link.Parameters,
                    CurrentRecord = currentRecord,
                    ExecutionTimestamp = now,
                    OperatingDate = DateOnly.FromDateTime(now.UtcDateTime),
                    CancellationToken = cancellationToken,
                },
                cancellationToken).ConfigureAwait(false);
            if (transformed.IsFailure)
                return GenericResult<object?>.Failure(FieldMappingTransformationResolverLog.TransformFailed(ctx.Logger, link.Name, field));

            value = transformed.Value;
        }

        return GenericResult<object?>.Success(value);
    }

    /// <summary>
    /// Applies calculated fields (expressions/calculators defined on DataSetFieldConfiguration) to each
    /// row in the result. Only operates on row-enumerable T values; non-collection types return a
    /// failure result.
    /// </summary>
    internal static IGenericResult<T> ApplyCalculatedFields<T>(
        DataSetExecutionContext ctx,
        IGenericResult<T> sourceResult,
        List<DataSetFieldConfiguration> calculatedFields,
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
        List<DataSetFieldConfiguration> calculatedFields,
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

    internal static IGenericResult<Dictionary<string, object?>> ObjectToDictionary(object obj)
    {
        if (obj is IDataRow row)
            return GenericResult<Dictionary<string, object?>>.Success(
                new Dictionary<string, object?>(row.AsDictionary(), StringComparer.OrdinalIgnoreCase));

        if (obj is Dictionary<string, object?> dict)
            return GenericResult<Dictionary<string, object?>>.Success(
                new Dictionary<string, object?>(dict, StringComparer.OrdinalIgnoreCase));

        var mapper = PocoMapperCollection.ByName(obj.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<Dictionary<string, object?>>.Failure(
                DataServiceResultCodes.ByName("MapperNotFound"),
                ResultDetails.Create("TypeName", obj.GetType().Name));

        return GenericResult<Dictionary<string, object?>>.Success(
            new Dictionary<string, object?>(mapper.MapToParameters(obj), StringComparer.OrdinalIgnoreCase));
    }

    internal static IReadOnlyList<IDataRow> DictionariesToRows(List<Dictionary<string, object?>> dictionaries)
        => DataRow.FromDictionaries(dictionaries.ConvertAll(d => (IDictionary<string, object?>)d));

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

        if (itemType == typeof(IDataRow) || itemType == typeof(DataRow))
            return GenericResult<T>.Success((T)(object)DictionariesToRows(dictionaries));

        if (itemType == typeof(Dictionary<string, object?>) || itemType == typeof(IDictionary<string, object?>))
            return GenericResult<T>.Success((T)(object)dictionaries);

        if (itemType == typeof(object))
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
