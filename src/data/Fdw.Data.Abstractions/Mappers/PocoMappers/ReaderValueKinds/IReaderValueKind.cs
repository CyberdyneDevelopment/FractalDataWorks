using System;
using Fdw.Collections;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>One kind of value a record dictionary holds, and how each typed getter of the reader reads it.</summary>
/// <remarks>The getters follow the data reader contract: a value that cannot be read as the type fails with the exception a data reader throws.</remarks>
public interface IReaderValueKind : ITypeOption<int, ReaderValueKindBase>
{
    /// <summary>Tells whether a value is of this kind.</summary>
    /// <param name="value">The value; a database null is <see cref="DBNull"/>.</param>
    /// <returns>True when this kind holds the value.</returns>
    bool Holds(object value);

    /// <summary>Reads the value as a GUID.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <param name="column">The column name, for a failure.</param>
    /// <returns>The GUID.</returns>
    Guid ToGuid(object value, string column);

    /// <summary>Reads the value as text.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <param name="column">The column name, for a failure.</param>
    /// <returns>The text.</returns>
    string ToText(object value, string column);

    /// <summary>Reads the value as a boolean.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The boolean.</returns>
    bool ToBoolean(object value);

    /// <summary>Reads the value as a date and time.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The date and time.</returns>
    DateTime ToDateTime(object value);

    /// <summary>Reads the value as a decimal.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The decimal.</returns>
    decimal ToDecimal(object value);

    /// <summary>Reads the value as a date and time with an offset.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <param name="column">The column name, for a failure.</param>
    /// <returns>The date and time with an offset.</returns>
    DateTimeOffset ToDateTimeOffset(object value, string column);
}
