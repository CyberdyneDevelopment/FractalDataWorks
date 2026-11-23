using System;
using Fdw.Collections;

namespace Fdw.Data.MsSql;

/// <summary>Reads one value type as the date a bulk copy writes to a date column; the collection is keyed by the value type's name.</summary>
public interface IMsSqlBulkCopyDateReader : ITypeOption<int, IMsSqlBulkCopyDateReader>
{
    /// <summary>The date and time the value stands for.</summary>
    /// <param name="value">The value, of the type this reader is named for.</param>
    /// <returns>The date and time, or <see langword="null"/> when the value is not a date (text that is not a date).</returns>
    DateTime? Moment(object value);

    /// <summary>The day the value stands for.</summary>
    /// <param name="value">The value, of the type this reader is named for.</param>
    /// <returns>The day, or <see langword="null"/> when the value is not a date.</returns>
    DateOnly? Day(object value);
}
