using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Connections.RowQuery.DateValues;

/// <summary>The type the date value reader collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class DateValueReaderBase : TypeOptionBase<DateValueReaderBase>, IDateValueReader
{
    /// <summary>Initializes the reader with the name of the value type it reads.</summary>
    /// <param name="name">The value type's name.</param>
    protected DateValueReaderBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract DateTimeOffset Read(object value);
}
