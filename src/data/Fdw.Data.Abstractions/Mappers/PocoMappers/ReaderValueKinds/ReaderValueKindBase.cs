using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>
/// The base of every record dictionary value kind. A getter a kind does not state reads the value as the data reader contract does:
/// through <see cref="Convert"/> in the invariant culture, which fails with an <see cref="InvalidCastException"/>, a
/// <see cref="FormatException"/> or an <see cref="OverflowException"/> for a value that is not of the type; a GUID and a date and time
/// with an offset have no such conversion, so a kind that does not state them fails.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ReaderValueKindBase : TypeOptionBase<ReaderValueKindBase>, IReaderValueKind
{
    /// <summary>Initializes a new instance of the <see cref="ReaderValueKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    protected ReaderValueKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public virtual Guid ToGuid(object value, string column) =>
        throw new InvalidCastException($"Cannot convert value of type '{value.GetType().Name}' to Guid for column '{column}'.");

    /// <inheritdoc />
    public virtual string ToText(object value, string column) =>
        Convert.ToString(value, CultureInfo.InvariantCulture)
            ?? throw new InvalidCastException($"Cannot convert value of type '{value.GetType().Name}' to string for column '{column}'.");

    /// <inheritdoc />
    public virtual bool ToBoolean(object value) => Convert.ToBoolean(value, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public virtual DateTime ToDateTime(object value) => Convert.ToDateTime(value, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public virtual decimal ToDecimal(object value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public virtual DateTimeOffset ToDateTimeOffset(object value, string column) =>
        throw new InvalidCastException($"Cannot convert value of type '{value.GetType().Name}' to DateTimeOffset for column '{column}'.");
}
