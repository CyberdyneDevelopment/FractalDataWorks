using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>The base of every record value kind.</summary>
/// <remarks>
/// Only the types JSON itself has are written natively; every other kind is written as its canonical text, the same text every
/// other format writes, so a value cannot mean one thing in a CSV export and another in a JSON one.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class RecordValueKindBase : TypeOptionBase<RecordValueKindBase>, IRecordValueKind
{
    /// <summary>Initializes a new instance of the <see cref="RecordValueKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    protected RecordValueKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract string Text(object value);

    /// <inheritdoc />
    public virtual void WriteJson(Utf8JsonWriter writer, object value) => writer.WriteStringValue(Text(value));

    /// <summary>Writes a number or a value that formats like one in the invariant culture.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The invariant text.</returns>
    protected static string Invariant(object value) => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
}
