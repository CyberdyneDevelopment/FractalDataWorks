using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RowQuery.DateValues;

/// <summary>Reads a <see cref="DateTime"/> as a <see cref="DateTimeOffset"/>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DateValueReaders), "DateTime")]
public sealed class DateTimeValueReader : DateValueReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeValueReader"/> class.</summary>
    public DateTimeValueReader()
        : base("DateTime")
    {
    }

    /// <inheritdoc/>
    public override DateTimeOffset Read(object value) =>
        value is DateTime dateTime ? new DateTimeOffset(dateTime)
            : throw new InvalidOperationException($"Unsupported date/time value type '{value.GetType().Name}'.");
}
