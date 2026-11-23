using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RowQuery.DateValues;

/// <summary>Reads a <see cref="DateTimeOffset"/> as a <see cref="DateTimeOffset"/>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DateValueReaders), "DateTimeOffset")]
public sealed class DateTimeOffsetValueReader : DateValueReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetValueReader"/> class.</summary>
    public DateTimeOffsetValueReader()
        : base("DateTimeOffset")
    {
    }

    /// <inheritdoc/>
    public override DateTimeOffset Read(object value) =>
        value is DateTimeOffset dateTimeOffset ? dateTimeOffset
            : throw new InvalidOperationException($"Unsupported date/time value type '{value.GetType().Name}'.");
}
