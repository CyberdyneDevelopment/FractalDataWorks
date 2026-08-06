using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.RowSources;

/// <summary>
/// Pre-computed mapping context for efficient row-to-dictionary conversion.
/// Caches field ordinals, names, and converters ONCE per read operation.
/// </summary>
public sealed class RowMappingContext : RowMappingContextBase
{
    private RowMappingContext(int[] ordinals, string[] names, IDataTypeConverter?[] converters)
        : base(ordinals, names, converters)
    {
    }

    /// <summary>
    /// Creates a mapping context from a row source and container schema.
    /// This method should be called ONCE before reading any rows.
    /// </summary>
    /// <param name="source">The record cursor to read from.</param>
    /// <param name="container">The container with schema metadata.</param>
    /// <param name="converterCollection">Optional converter collection for type conversion lookup.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    /// <returns>A pre-computed mapping context.</returns>
    public static RowMappingContext Create(
        IRecordCursor source,
        IStorageContainer container,
        IDataTypeConverters? converterCollection = null,
        ILogger? logger = null)
    {
        var fields = container.Schema.Fields;
        var count = fields.Count;
        var ordinals = new int[count];
        var names = new string[count];
        var converters = new IDataTypeConverter?[count];

        for (int i = 0; i < count; i++)
        {
            var field = fields[i];
            names[i] = field.Name;

            // Get ordinal from source
            ordinals[i] = source.GetFieldOrdinal(field.Name);

            // Log if field not found (ordinal -1)
            if (ordinals[i] < 0 && logger != null)
            {
                RowSourceLog.FieldNotFound(logger, field.Name);
            }

            // Look up converter if we have a converter collection and field has type info
            if (converterCollection != null && field.ConverterTypeId.HasValue)
            {
                converters[i] = converterCollection.ById(field.ConverterTypeId.Value);
            }
        }

        return new RowMappingContext(ordinals, names, converters);
    }
}
