using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql;

/// <summary>Reads a <see cref="string"/> as the date a bulk copy writes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlBulkCopyDateReaders), "String")]
public sealed class StringMsSqlBulkCopyDateReader : MsSqlBulkCopyDateReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="StringMsSqlBulkCopyDateReader"/> class.</summary>
    public StringMsSqlBulkCopyDateReader()
        : base("String")
    {
    }

    /// <inheritdoc/>
    public override DateTime? Moment(object value) => value is string text && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
}
