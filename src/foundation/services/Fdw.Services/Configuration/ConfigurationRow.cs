using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fdw.Services.Configuration;

/// <summary>
/// One row of a configuration container, read as the columns the store returned, with typed reads of a column.
/// </summary>
/// <remarks>
/// A column is found without regard to case. A null or database null is nothing; a column the row does not have is not nothing, it is
/// a fault in the query, so reading it throws and the reader that asked reports it.
/// </remarks>
public sealed class ConfigurationRow
{
    private readonly Dictionary<string, object?> _columns;

    /// <summary>Initializes a new instance of the <see cref="ConfigurationRow"/> class.</summary>
    /// <param name="columns">The columns the store returned.</param>
    public ConfigurationRow(IReadOnlyDictionary<string, object?> columns)
    {
        _columns = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in columns)
        {
            _columns[column.Key] = column.Value is DBNull ? null : column.Value;
        }
    }

    /// <summary>Reads a text column.</summary>
    /// <param name="column">The column's name.</param>
    /// <returns>The text, or nothing when the column is null.</returns>
    public string? Text(string column) => Value(column) is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    /// <summary>Reads a whole-number column.</summary>
    /// <param name="column">The column's name.</param>
    /// <returns>The number, or nothing when the column is null.</returns>
    public long? Number(string column) => Value(column) is IConvertible value ? Convert.ToInt64(value, CultureInfo.InvariantCulture) : null;

    /// <summary>Reads an identifier column.</summary>
    /// <param name="column">The column's name.</param>
    /// <returns>The identifier, or nothing when the column is null.</returns>
    /// <exception cref="InvalidOperationException">The column holds text that is not an identifier.</exception>
    public Guid? Identifier(string column)
        => Value(column) switch
        {
            null => null,
            Guid identifier => identifier,
            var other => Guid.TryParse(Convert.ToString(other, CultureInfo.InvariantCulture), out var parsed)
                ? parsed
                : throw new InvalidOperationException($"Column '{column}' holds '{other}', which is not an identifier"),
        };

    /// <summary>Reads a date-and-time column.</summary>
    /// <param name="column">The column's name.</param>
    /// <returns>The moment, or nothing when the column is null.</returns>
    /// <exception cref="InvalidOperationException">The column holds text that is not a moment.</exception>
    public DateTimeOffset? Moment(string column)
        => Value(column) switch
        {
            null => null,
            DateTimeOffset moment => moment,
            DateTime moment => new DateTimeOffset(moment),
            var other => DateTimeOffset.TryParse(Convert.ToString(other, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : throw new InvalidOperationException($"Column '{column}' holds '{other}', which is not a date and time"),
        };

    /// <summary>Reads a true-or-false column.</summary>
    /// <param name="column">The column's name.</param>
    /// <returns>The flag, or nothing when the column is null.</returns>
    /// <exception cref="InvalidOperationException">The column holds text that is not true or false.</exception>
    public bool? Flag(string column)
        => Value(column) switch
        {
            null => null,
            bool flag => flag,
            var other => bool.TryParse(Convert.ToString(other, CultureInfo.InvariantCulture), out var parsed)
                ? parsed
                : throw new InvalidOperationException($"Column '{column}' holds '{other}', which is not true or false"),
        };

    private object? Value(string column)
        => _columns.TryGetValue(column, out var value)
            ? value
            : throw new InvalidOperationException($"The row has no column '{column}'; the query did not return it");
}
