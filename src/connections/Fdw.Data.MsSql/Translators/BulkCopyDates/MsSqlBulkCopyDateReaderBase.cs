using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.MsSql;

/// <summary>The type the bulk copy date reader collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class MsSqlBulkCopyDateReaderBase : TypeOptionBase<MsSqlBulkCopyDateReaderBase>, IMsSqlBulkCopyDateReader
{
    /// <summary>Initializes the reader with the name of the value type it reads.</summary>
    /// <param name="name">The value type's name.</param>
    protected MsSqlBulkCopyDateReaderBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract DateTime? Moment(object value);

    /// <inheritdoc/>
    public virtual DateOnly? Day(object value) => Moment(value) is { } moment ? DateOnly.FromDateTime(moment) : null;
}
