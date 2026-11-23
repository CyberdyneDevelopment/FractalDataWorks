using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql;

/// <summary>Reads a <see cref="DateOnly"/> as the date a bulk copy writes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlBulkCopyDateReaders), "DateOnly")]
public sealed class DateOnlyMsSqlBulkCopyDateReader : MsSqlBulkCopyDateReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="DateOnlyMsSqlBulkCopyDateReader"/> class.</summary>
    public DateOnlyMsSqlBulkCopyDateReader()
        : base("DateOnly")
    {
    }

    /// <inheritdoc/>
    public override DateTime? Moment(object value) => value is DateOnly day ? day.ToDateTime(TimeOnly.MinValue) : null;

    /// <inheritdoc/>
    public override DateOnly? Day(object value) => value is DateOnly day ? day : null;
}
