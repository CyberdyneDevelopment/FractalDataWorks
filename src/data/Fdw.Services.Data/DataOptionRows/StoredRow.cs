using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Options;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataOptionRows;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.DataOptionRows;

/// <summary>Reads one row of the options table as the store answered it (FDW-5664).</summary>
/// <remarks>A field the row lacks, or holds as the wrong kind, is a coded failure naming it; nothing is filled in.</remarks>
internal static class StoredRow
{
    /// <summary>Reads a row.</summary>
    /// <param name="row">The row, keyed by field name in any case.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <returns>The row, or the failure naming the first field it could not read.</returns>
    internal static IGenericResult<DataOptionRow> Read(Dictionary<string, object?> row, ILogger logger)
    {
        if (Field(row, DataOptionRowFields.Id) is not Guid id)
            return Unreadable(logger, DataOptionRowFields.Id);

        if (Field(row, DataOptionRowFields.Active) is not bool active)
            return Unreadable(logger, DataOptionRowFields.Active);

        string[] names = [DataOptionRowFields.DataStore, DataOptionRowFields.Container, DataOptionRowFields.Column, DataOptionRowFields.Value, DataOptionRowFields.OptionType, DataOptionRowFields.Option];
        var text = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (Field(row, name) is not string value)
                return Unreadable(logger, name);

            text[name] = value;
        }

        return GenericResult<DataOptionRow>.Success(new DataOptionRow
        {
            Id = id,
            DataStore = text[DataOptionRowFields.DataStore],
            Container = text[DataOptionRowFields.Container],
            Column = text[DataOptionRowFields.Column],
            Value = text[DataOptionRowFields.Value],
            OptionType = text[DataOptionRowFields.OptionType],
            Option = text[DataOptionRowFields.Option],
            Active = active,
        });
    }

    private static object? Field(Dictionary<string, object?> row, string name)
        => row.FirstOrDefault(entry => string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static IGenericResult<DataOptionRow> Unreadable(ILogger logger, string field)
        => GenericResult<DataOptionRow>.Failure(DataOptionRowsLog.StoredRowUnreadable(logger, field));
}
