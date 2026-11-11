using Fdw.Results;
using Fdw.Data.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Data.RowSources.Delimited.Xlsx;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Delimited;

/// <summary>The Excel workbook (.xlsx) record source format, the counterpart of <see cref="XlsxRowWriterType"/>.</summary>
/// <remarks>
/// A binary format: its content is the file's own bytes, not decoded text, so <see cref="ReadsBinary"/> is true and a caller
/// that holds a file reads it as bytes. The columns come from <see cref="XlsxRowSourceOptions"/>; a workbook with several
/// worksheets must name the one to read.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordSourceTypes), "Xlsx")]
public sealed class XlsxRowSourceType : RecordSourceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="XlsxRowSourceType"/> class.</summary>
    public XlsxRowSourceType() : base("Xlsx")
    {
        BufferSize = 16 * 1024;
    }

    /// <inheritdoc />
    /// <remarks>Reads the sheet through a ZipArchive over a seekable stream, its rows through an XmlReader. (FDW-4967)</remarks>
    public override bool ParsesIncrementally => true;

    /// <inheritdoc />
    public override bool SupportsSync => true;

    /// <inheritdoc />
    public override bool SupportsAsync => false;

    /// <inheritdoc />
    public override bool SupportsReset => false;

    /// <inheritdoc />
    public override int TypicalAllocationsPerRow => 1;

    /// <inheritdoc />
    public override string Format => "Xlsx";

    /// <inheritdoc />
    public override bool ReadsBinary => true;

    /// <inheritdoc />
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions options)
        => new XlsxRowSource(content, options as XlsxRowSourceOptions
            ?? throw new ArgumentException("The Xlsx format reads with XlsxRowSourceOptions.", nameof(options)));

    /// <inheritdoc />
    public override IGenericResult<RowSourceOptions> CreateOptions(IStorageContainer container)
    {
        var choices = Choices(container);
        // Why the columns are the container's field order (FDW-1308): the reader names its columns left to right from column A. Leaving
        // the sheet out is itself the choice of a workbook's only sheet, and the reader refuses a workbook of several.
        return choices.Result<RowSourceOptions>(new XlsxRowSourceOptions
        {
            ContinueOnError = choices.Flag(ContinueOnErrorKey),
            HasHeader = choices.Flag("HasHeader"),
            SheetName = choices.OptionalText("SheetName"),
            Columns = ContainerRowMetadata.FieldNames(container),
            BufferSize = BufferSize,
        });
    }

    /// <inheritdoc />
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => CreateRowSource(context);
}
