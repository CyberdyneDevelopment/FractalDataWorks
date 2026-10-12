using System;
using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Base class for pre-computed mapping context that caches field ordinals and converters.
/// Derived classes add source-specific initialization.
/// </summary>
public abstract class RowMappingContextBase
{
    /// <summary>
    /// Pre-computed ordinals for each field. -1 indicates field not found in source.
    /// </summary>
    public int[] FieldOrdinals { get; }

    /// <summary>
    /// Field names matching the ordinal arrays.
    /// </summary>
    public string[] FieldNames { get; }

    /// <summary>
    /// Data type converters for each field, if available.
    /// Null entries indicate no conversion needed.
    /// </summary>
    public IDataTypeConverter?[] FieldConverters { get; }

    /// <summary>
    /// Number of fields to process.
    /// </summary>
    public int FieldCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RowMappingContextBase"/> class.
    /// </summary>
    /// <param name="ordinals">Pre-computed ordinal array.</param>
    /// <param name="names">Field names array.</param>
    /// <param name="converters">Type converters array (can contain nulls).</param>
    protected RowMappingContextBase(int[] ordinals, string[] names, IDataTypeConverter?[] converters)
    {
        if (ordinals.Length != names.Length || ordinals.Length != converters.Length)
        {
            throw new ArgumentException("Arrays must have the same length.", nameof(ordinals));
        }

        FieldOrdinals = ordinals;
        FieldNames = names;
        FieldConverters = converters;
        FieldCount = names.Length;
    }

    /// <summary>
    /// Creates a mapping context from a row source and container schema.
    /// </summary>
    /// <param name="source">The record cursor to build context from.</param>
    /// <param name="container">The container with schema metadata.</param>
    /// <param name="converterCollection">Optional converter collection for type conversion lookup.</param>
    /// <returns>A pre-computed mapping context.</returns>
    public static RowMappingContextBase Create(
        IRecordCursor source,
        IStorageContainer container,
        IDataTypeConverters? converterCollection = null)
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

            // Look up converter if we have a converter collection and field has type info
            if (converterCollection != null && field.ConverterTypeId.HasValue)
            {
                converters[i] = converterCollection.ById(field.ConverterTypeId.Value);
            }
        }

        return new DefaultRowMappingContext(ordinals, names, converters);
    }
}