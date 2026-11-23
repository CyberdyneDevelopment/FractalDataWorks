using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql;

/// <summary>Reads a <see cref="DateTimeOffset"/> as the date a bulk copy writes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlBulkCopyDateReaders), "DateTimeOffset")]
public sealed class DateTimeOffsetMsSqlBulkCopyDateReader : MsSqlBulkCopyDateReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetMsSqlBulkCopyDateReader"/> class.</summary>
    public DateTimeOffsetMsSqlBulkCopyDateReader()
        : base("DateTimeOffset")
    {
    }

    /// <inheritdoc/>
    public override DateTime? Moment(object value) => value is DateTimeOffset moment ? moment.DateTime : null;
}
