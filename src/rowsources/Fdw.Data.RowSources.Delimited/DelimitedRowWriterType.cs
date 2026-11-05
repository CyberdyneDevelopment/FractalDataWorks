using Fdw.Results;
using Fdw.Data.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Delimited;

/// <summary>
/// TypeOption for delimited (CSV / variable-length) row writers, backed by RecordParser.
/// The write-side mirror of <see cref="DelimitedRowSourceType"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordWriterTypes), "Delimited")]
public sealed class DelimitedRowWriterType : RecordWriterTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedRowWriterType"/> class.
    /// </summary>
    public DelimitedRowWriterType() : base("Delimited")
    {
        BufferSize = 16 * 1024;
    }

    /// <inheritdoc />
    public override string Format => "Delimited";

    /// <inheritdoc />
    public override string MediaType => "text/csv";

    /// <inheritdoc />
    public override string FileExtension => "csv";

    /// <inheritdoc />
    public override IGenericResult<RowWriterOptions> CreateOptions(IReadOnlyList<string> columns, IReadOnlyDictionary<string, object> stated, string owner)
    {
        var choices = new RowFormatChoices(stated, owner, Format);
        // Why: the columns come in a stable order from whoever asks for the rows, never inferred from the first row.
        return choices.Result<RowWriterOptions>(new DelimitedRowWriterOptions
        {
            Columns = [.. columns],
            WriteHeader = choices.Flag("HasHeader"),
            Separator = choices.Text("Separator"),
            QuoteChar = choices.Character("QuoteChar"),
            WriteByteOrderMark = choices.Flag("WriteByteOrderMark"),
            NeutralizeFormulas = choices.Flag("NeutralizeFormulas"),
            BufferSize = BufferSize,
        });
    }


    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions options)
        => new DelimitedStreamRowWriter(target, options as DelimitedRowWriterOptions ?? throw new ArgumentException("The Delimited format writes with DelimitedRowWriterOptions.", nameof(options)));
}
