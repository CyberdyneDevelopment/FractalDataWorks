using Fdw.Data.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Data.RowSources.Delimited.Xlsx;
using Fdw.Results;

namespace Fdw.Data.RowSources.Delimited;

/// <summary>The Excel workbook (.xlsx) record writer format (FDW-1298).</summary>
/// <remarks>
/// Lives beside the delimited format, not in a package of its own: Excel is the spreadsheet counterpart of CSV, and every host that
/// serves CSV then serves Excel with no further package. It is a binary format: <see cref="CreateWriter"/> refuses, and
/// <see cref="CreateBinaryWriter"/> is the writer.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordWriterTypes), "Xlsx")]
public sealed class XlsxRowWriterType : RecordWriterTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="XlsxRowWriterType"/> class.</summary>
    public XlsxRowWriterType() : base("Xlsx")
    {
        BufferSize = 16 * 1024;
        RowsPerSheet = XlsxRowWriterOptions.MaxRowsPerSheet;
    }

    /// <inheritdoc />
    public override string Format => "Xlsx";

    /// <inheritdoc />
    public override string MediaType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <inheritdoc />
    public override string FileExtension => "xlsx";

    /// <inheritdoc />
    public override bool WritesBinary => true;

    /// <summary>Gets or sets the rows a worksheet holds, the header row included, before the rows continue on a further worksheet. Declared here
    /// as this format's own value (FDW-3823), at most <see cref="XlsxRowWriterOptions.MaxRowsPerSheet"/>; a host may change it at registration.</summary>
    public int RowsPerSheet { get; set; }

    /// <inheritdoc />
    /// <remarks>Why the columns come from the caller in a stable order (FDW-1298): a workbook's header row and its cells follow them,
    /// and the writer refuses an empty list.</remarks>
    public override IGenericResult<RowWriterOptions> CreateOptions(IReadOnlyList<string> columns, IReadOnlyDictionary<string, object> stated, string owner)
    {
        var choices = new RowFormatChoices(stated, owner, Format);
        return choices.Result<RowWriterOptions>(new XlsxRowWriterOptions
        {
            Columns = [.. columns],
            SheetName = choices.Text("SheetName"),
            BufferSize = BufferSize,
            RowsPerSheet = RowsPerSheet,
        });
    }

    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions options)
        => throw new NotSupportedException("The Xlsx format is a zip of XML parts, not text; it is written to a byte stream with CreateBinaryWriter.");

    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateBinaryWriter(Stream target, RowWriterOptions options)
        => new XlsxRowWriter(target, options as XlsxRowWriterOptions ?? throw new ArgumentException("The Xlsx format writes with XlsxRowWriterOptions.", nameof(options)));
}
