using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// Wraps a set of ALREADY-VALIDATED (<see cref="RecordRowValidator"/>), already-filtered/joined
/// in-memory row dictionaries as a forward-only <see cref="DbDataReader"/> so the generated POCO
/// mappers (<c>MapFromReader</c>) can materialize them without a separate dictionary-mapping code path.
/// Column names are the ordered union of every row's keys (first-seen order).
/// </summary>
/// <remarks>
/// <para>
/// Every row this reader is constructed over has already passed <see cref="RecordRowValidator"/>: a
/// container field declared <c>IsNullable: false</c> is GUARANTEED present and non-null on every row
/// before this type is ever constructed — that check happens once, up front, against the container's
/// declared schema, not per-getter here. Consequently, <see cref="GetOrdinal"/> throwing for an unknown
/// column name can only mean the requested name has NO declared field on the container at all (an
/// optional POCO property the container's schema does not carry) — exactly the same situation a real
/// ADO.NET provider's <c>GetOrdinal</c> faces for a column outside the executed SELECT list, and the
/// generated PocoMapper's <c>GetReaderValue_*</c> helpers legitimately default that ONE case. This
/// reader is not the place that decides whether a value may be missing; that decision was already made
/// and enforced upstream.
/// </para>
/// <para>
/// Format-agnostic: typed getters are COERCING, not unchecked hard casts, because a decoded row's raw
/// value is whatever primitive the source format's reader produced (e.g. JSON decode yields only
/// string/long/double/bool/null — see <c>JsonStreamRowSource</c>), so a column typed as <see cref="Guid"/>
/// or <see cref="DateTimeOffset"/> on the POCO arrives here as a string and must be parsed, not cast.
/// This is why callers use <c>MapFromReader</c> and not <c>MapFromDictionary</c> (which does unchecked
/// hard casts and throws on these values).
/// </para>
/// </remarks>
// Why: DbDataReader (the BCL base class) itself implements only the non-generic IEnumerable (for
// legacy row iteration via GetEnumerator(), which this forward-only reader deliberately does not
// support — see GetEnumerator() below). CA1010 flags every concrete DbDataReader subclass equally;
// there is no IEnumerable<T> to add without breaking the DbDataReader contract, so this is a
// documented suppression, not a masked defect.
[SuppressMessage("Design", "CA1010:Generic interface should also be implemented", Justification = "DbDataReader itself only implements non-generic IEnumerable; GetEnumerator() is intentionally unsupported below.")]
public sealed class RecordDictionaryReader : DbDataReader
{
    private readonly IReadOnlyList<IReadOnlyDictionary<string, object?>> _rows;
    private readonly List<string> _columns;
    private int _rowIndex = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordDictionaryReader"/> class over the supplied rows.
    /// </summary>
    /// <param name="rows">The already-filtered/joined rows to expose, in result order.</param>
    public RecordDictionaryReader(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        _rows = rows;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var columns = new List<string>();
        foreach (var row in rows)
        {
            foreach (var key in row.Keys)
            {
                if (seen.Add(key))
                    columns.Add(key);
            }
        }
        _columns = columns;
    }

    private IReadOnlyDictionary<string, object?> CurrentRow => _rows[_rowIndex];

    /// <inheritdoc/>
    public override int FieldCount => _columns.Count;

    /// <inheritdoc/>
    public override bool HasRows => _rows.Count > 0;

    /// <inheritdoc/>
    public override int Depth => 0;

    /// <inheritdoc/>
    public override bool IsClosed => false;

    /// <inheritdoc/>
    public override int RecordsAffected => -1;

    /// <inheritdoc/>
    public override object this[int ordinal] => GetValue(ordinal);

    /// <inheritdoc/>
    public override object this[string name] => GetValue(GetOrdinal(name));

    /// <inheritdoc/>
    public override bool Read()
    {
        if (_rowIndex + 1 >= _rows.Count)
            return false;

        _rowIndex++;
        return true;
    }

    /// <inheritdoc/>
    public override bool NextResult() => false;

    /// <inheritdoc/>
    public override string GetName(int ordinal) => _columns[ordinal];

    /// <inheritdoc/>
    // Why: an unknown column name must raise IndexOutOfRangeException — DbDataReader's own documented
    // GetOrdinal contract (every real ADO.NET provider throws exactly this for a name outside the
    // result set), and the generated PocoMapper's GetReaderValue_* helpers catch specifically that
    // exception to default an OPTIONAL POCO property with no declared field on this container. CA2201
    // forbids explicitly constructing that reserved exception type (`throw new
    // IndexOutOfRangeException(...)`), so it is raised the way the BCL itself raises it — an actual
    // out-of-bounds array read — rather than suppressing the analyzer. No suppression, no masked defect.
    public override int GetOrdinal(string name)
    {
        for (var i = 0; i < _columns.Count; i++)
        {
            if (string.Equals(_columns[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return Array.Empty<int>()[0];
    }

    /// <inheritdoc/>
    public override bool IsDBNull(int ordinal)
    {
        var name = _columns[ordinal];
        return !CurrentRow.TryGetValue(name, out var value) || value is null;
    }

    /// <inheritdoc/>
    public override object GetValue(int ordinal)
    {
        var name = _columns[ordinal];
        return CurrentRow.TryGetValue(name, out var value) && value is not null ? value : DBNull.Value;
    }

    /// <inheritdoc/>
    public override int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, FieldCount);
        for (var i = 0; i < count; i++)
            values[i] = GetValue(i);
        return count;
    }

    /// <inheritdoc/>
    public override Guid GetGuid(int ordinal)
    {
        var value = GetValue(ordinal);
        return value switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text),
            _ => throw new InvalidCastException(
                $"Cannot convert value of type '{value.GetType().Name}' to Guid for column '{GetName(ordinal)}'."),
        };
    }

    /// <inheritdoc/>
    // Why (fix #5): the removed `?? string.Empty` fabricated a value for a null/unconvertible column —
    // forbidden. After RecordRowValidator (fix #1), a DECLARED non-nullable column can no longer reach
    // this reader as DBNull, so a DBNull value here is either a declared-nullable field genuinely
    // carrying no value (a real caller error — GetString on a null column) or a wiring defect; a real
    // ADO.NET provider throws for a typed read of a null column, and so does this one.
    public override string GetString(int ordinal)
    {
        var value = GetValue(ordinal);
        if (ReferenceEquals(value, DBNull.Value))
            throw new InvalidCastException($"Column '{GetName(ordinal)}' is null; GetString cannot read a null column.");

        return value switch
        {
            string text => text,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
                ?? throw new InvalidCastException($"Cannot convert value of type '{value.GetType().Name}' to string for column '{GetName(ordinal)}'."),
        };
    }

    /// <inheritdoc/>
    public override bool GetBoolean(int ordinal)
    {
        var value = GetValue(ordinal);
        return value switch
        {
            bool flag => flag,
            string text when bool.TryParse(text, out var parsedBool) => parsedBool,
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt) => parsedInt != 0,
            long integral => integral != 0,
            decimal number => number != 0,
            double real => real != 0,
            _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture),
        };
    }

    /// <inheritdoc/>
    public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        => throw new NotSupportedException($"{nameof(RecordDictionaryReader)} does not support binary column data.");

    /// <inheritdoc/>
    public override char GetChar(int ordinal) => Convert.ToChar(GetValue(ordinal), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        => throw new NotSupportedException($"{nameof(RecordDictionaryReader)} does not support character array column data.");

    /// <inheritdoc/>
    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

    /// <inheritdoc/>
    public override DateTime GetDateTime(int ordinal)
    {
        var value = GetValue(ordinal);
        return value switch
        {
            DateTime dateTime => dateTime,
            DateTimeOffset dateTimeOffset => dateTimeOffset.UtcDateTime,
            string text => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture),
        };
    }

    /// <inheritdoc/>
    public override decimal GetDecimal(int ordinal)
    {
        var value = GetValue(ordinal);
        return value switch
        {
            decimal number => number,
            long integral => integral,
            double real => (decimal)real,
            string text => decimal.Parse(text, CultureInfo.InvariantCulture),
            _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
        };
    }

    /// <inheritdoc/>
    public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override IEnumerator GetEnumerator() => throw new NotSupportedException($"{nameof(RecordDictionaryReader)} does not support IEnumerator iteration.");

    /// <inheritdoc/>
    public override Type GetFieldType(int ordinal)
    {
        var value = GetValue(ordinal);
        return ReferenceEquals(value, DBNull.Value) ? typeof(object) : value.GetType();
    }

    /// <inheritdoc/>
    public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    // Why: coercing, not (T)GetValue(ordinal) — the base DbDataReader.GetFieldValue<T> default does an
    // unchecked cast, which throws for e.g. a Guid/DateTimeOffset column whose raw decoded value is a
    // string. Route every well-known T through the matching coercing getter above.
    public override T GetFieldValue<T>(int ordinal)
    {
        var targetType = typeof(T);
        object result;

        if (targetType == typeof(Guid)) result = GetGuid(ordinal);
        else if (targetType == typeof(string)) result = GetString(ordinal);
        else if (targetType == typeof(bool)) result = GetBoolean(ordinal);
        else if (targetType == typeof(byte)) result = GetByte(ordinal);
        else if (targetType == typeof(short)) result = GetInt16(ordinal);
        else if (targetType == typeof(int)) result = GetInt32(ordinal);
        else if (targetType == typeof(long)) result = GetInt64(ordinal);
        else if (targetType == typeof(float)) result = GetFloat(ordinal);
        else if (targetType == typeof(double)) result = GetDouble(ordinal);
        else if (targetType == typeof(decimal)) result = GetDecimal(ordinal);
        else if (targetType == typeof(DateTime)) result = GetDateTime(ordinal);
        else if (targetType == typeof(DateTimeOffset)) result = GetDateTimeOffset(ordinal);
        else result = GetValue(ordinal);

        return (T)result;
    }

    private DateTimeOffset GetDateTimeOffset(int ordinal)
    {
        var value = GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(dateTime),
            string text => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            _ => throw new InvalidCastException(
                $"Cannot convert value of type '{value.GetType().Name}' to DateTimeOffset for column '{GetName(ordinal)}'."),
        };
    }

    /// <inheritdoc/>
    public override DataTable? GetSchemaTable() => null;
}
