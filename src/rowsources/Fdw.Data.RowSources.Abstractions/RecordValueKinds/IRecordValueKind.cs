using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>One kind of value a record can hold, with its canonical text and the way a JSON writer writes it.</summary>
public interface IRecordValueKind : ITypeOption<int, RecordValueKindBase>
{
    /// <summary>Tells whether a record value is of this kind.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>True when this kind holds the value.</returns>
    bool Holds(object value);

    /// <summary>Writes a value of this kind in its canonical text form.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The canonical text.</returns>
    string Text(object value);

    /// <summary>Writes a value of this kind as a JSON value.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    void WriteJson(Utf8JsonWriter writer, object value);
}
