using System.Collections.Generic;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Reads the values of named fields from a record for the transforms that group or order by them.
/// </summary>
internal static class RecordKeyReader
{
    /// <summary>Reads one value per field, in field order.</summary>
    /// <param name="record">The record to read.</param>
    /// <param name="fields">The fields to read.</param>
    /// <param name="values">The values read, in field order, when every field is present.</param>
    /// <param name="missingField">The first field the record lacks, when one is missing.</param>
    /// <returns><see langword="true"/> when the record carries every field.</returns>
    public static bool TryRead(IDictionary<string, object?> record, List<string> fields, out object?[] values, out string? missingField)
    {
        values = new object?[fields.Count];
        for (var index = 0; index < fields.Count; index++)
        {
            if (!record.TryGetValue(fields[index], out var value))
            {
                missingField = fields[index];
                return false;
            }

            values[index] = value;
        }

        missingField = null;
        return true;
    }
}
