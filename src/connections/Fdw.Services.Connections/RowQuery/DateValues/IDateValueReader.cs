using System;
using Fdw.Collections;

namespace Fdw.Services.Connections.RowQuery.DateValues;

/// <summary>Reads one date and time type of a row value as a <see cref="DateTimeOffset"/>; the collection is keyed by the value type's name.</summary>
public interface IDateValueReader : ITypeOption<int, IDateValueReader>
{
    /// <summary>Reads the value.</summary>
    /// <param name="value">The value, of the type this reader is named for.</param>
    /// <returns>The value as a <see cref="DateTimeOffset"/>.</returns>
    DateTimeOffset Read(object value);
}
