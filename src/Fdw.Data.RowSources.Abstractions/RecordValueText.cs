using System;
using System.Globalization;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The canonical text form of a record value — the single definition every record writer shares.
/// </summary>
/// <remarks>
/// Each writer used to carry its own <c>Convert.ToString(value, InvariantCulture)</c>. That call
/// never fails; for a type it does not understand it returns the type's <c>ToString</c>, so a
/// <see cref="byte"/>[] column exported as the literal text "System.Byte[]" and the export still
/// reported success. Four copies of an incomplete list is why nobody noticed the list was
/// incomplete.
/// <para>
/// The forms here are chosen to be round-trippable and machine-readable rather than pretty: ISO 8601
/// for instants, base64 for bytes, EWKT for geometry. A consumer must be able to parse a value back
/// without knowing which machine wrote it or what its locale was.
/// </para>
/// </remarks>
public static class RecordValueText
{
    /// <summary>
    /// Converts a record value to its canonical text form.
    /// </summary>
    /// <param name="value">The value. Must not be null — a null has no text form and the caller decides what absence looks like in its format.</param>
    /// <param name="columnName">The column the value came from, used to name the column in a failure.</param>
    /// <returns>The canonical text.</returns>
    /// <exception cref="NotSupportedException">The value's type has no defined serialized form.</exception>
    public static string Format(object value, string columnName)
    {
        switch (value)
        {
            case string text: return text;
            case char character: return character.ToString();

            // Why lowercase rather than Boolean.ToString's "True": every other format this value
            // round-trips through - JSON, XML schema, most CSV readers - spells it lowercase.
            case bool flag: return flag ? "true" : "false";

            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                return ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);

            // "O" is the round-trip format: it keeps sub-second precision and the Kind/offset, both
            // of which a general-purpose format silently discards.
            case DateTime instant: return instant.ToString("O", CultureInfo.InvariantCulture);
            case DateTimeOffset instant: return instant.ToString("O", CultureInfo.InvariantCulture);
            case TimeSpan duration: return duration.ToString("c", CultureInfo.InvariantCulture);

            case Guid id: return id.ToString("D", CultureInfo.InvariantCulture);
            case byte[] bytes: return Convert.ToBase64String(bytes);

            // EWKT rather than bare WKT: the SRID is part of what the value MEANS, and a coordinate
            // pair without the reference system it was measured in is not the same value.
            case SpatialValue spatial:
                return string.Concat("SRID=", spatial.Srid.ToString(CultureInfo.InvariantCulture), ";", spatial.Wkt);

            default:
                // NO FALLBACK. Convert.ToString would return something here, and that something is
                // how "System.Byte[]" reached an export that reported success.
                throw new NotSupportedException(
                    $"Column '{columnName}' holds a {value.GetType().FullName}, which has no defined serialized form. " +
                    "Add it to RecordValueText.Format rather than letting it reach the output as its type name.");
        }
    }

    /// <summary>
    /// Gets whether a value is absent — null or <see cref="DBNull"/>.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> when the value is absent.</returns>
    /// <remarks>
    /// A data reader yields <see cref="DBNull"/> where a record yields null, and the two mean the
    /// same thing. Testing only for null lets DBNull through to be formatted as the text "".
    /// </remarks>
    public static bool IsAbsent(object? value) => value is null || value is DBNull;
}
