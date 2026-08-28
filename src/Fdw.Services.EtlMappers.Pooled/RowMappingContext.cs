using System;
using System.Collections.Generic;
using System.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.EtlMappers;
using Fdw.Services;
using Fdw;

namespace Fdw.Services.EtlMappers.Pooled;

/// <summary>
/// Pre-computed mapping context for efficient row-to-dictionary conversion.
/// Caches field ordinals and metadata ONCE per read operation.
/// </summary>
public sealed class RowMappingContext
{
    /// <summary>
    /// Pre-computed ordinals for each field. -1 indicates field not found in result set.
    /// </summary>
    public int[] FieldOrdinals { get; }

    /// <summary>
    /// Field names matching the ordinal arrays.
    /// </summary>
    public string[] FieldNames { get; }

    /// <summary>
    /// Number of fields to process.
    /// </summary>
    public int FieldCount { get; }

    private RowMappingContext(int[] ordinals, string[] names)
    {
        FieldOrdinals = ordinals;
        FieldNames = names;
        FieldCount = names.Length;
    }

    /// <summary>
    /// Creates a mapping context from a data reader and container schema.
    /// This method should be called ONCE before reading any rows.
    /// </summary>
    /// <param name="reader">The data reader to read from.</param>
    /// <param name="container">The container with schema metadata.</param>
    /// <returns>A pre-computed mapping context.</returns>
    public static RowMappingContext Create(IDataReader reader, IStorageContainer container)
    {
        var fields = container.Schema.Fields;
        var count = fields.Count;
        var ordinals = new int[count];
        var names = new string[count];

        for (int i = 0; i < count; i++)
        {
            var field = fields[i];
            names[i] = field.Name;

            // Pre-compute ordinal
            try
            {
                ordinals[i] = reader.GetOrdinal(field.Name);
            }
            catch (IndexOutOfRangeException ex)
            {
                _ = ex;
                ordinals[i] = -1;
            }
        }

        return new RowMappingContext(ordinals, names);
    }
}
