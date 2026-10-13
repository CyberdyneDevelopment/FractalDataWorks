using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Data.RowSources.Delimited.Xlsx;

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
    }

    /// <inheritdoc />
    public override string Format => "Xlsx";

    /// <inheritdoc />
    public override string MediaType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <inheritdoc />
    public override string FileExtension => "xlsx";

    /// <inheritdoc />
    public override bool WritesBinary => true;

    /// <inheritdoc />
    public override RowWriterOptions CreateOptions(IReadOnlyList<string> columns)
        => new XlsxRowWriterOptions { Columns = [.. columns] };

    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions? options)
        => throw new NotSupportedException("The Xlsx format is a zip of XML parts, not text; it is written to a byte stream with CreateBinaryWriter.");

    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateBinaryWriter(Stream target, RowWriterOptions? options)
        => new XlsxRowWriter(target, options as XlsxRowWriterOptions);
}
