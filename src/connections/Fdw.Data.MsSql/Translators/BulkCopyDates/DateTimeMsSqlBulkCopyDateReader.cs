using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql;

/// <summary>Reads a <see cref="DateTime"/> as the date a bulk copy writes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlBulkCopyDateReaders), "DateTime")]
public sealed class DateTimeMsSqlBulkCopyDateReader : MsSqlBulkCopyDateReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeMsSqlBulkCopyDateReader"/> class.</summary>
    public DateTimeMsSqlBulkCopyDateReader()
        : base("DateTime")
    {
    }

    /// <inheritdoc/>
    public override DateTime? Moment(object value) => value is DateTime moment ? moment : null;
}
