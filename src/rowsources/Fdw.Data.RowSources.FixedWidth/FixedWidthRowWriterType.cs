using Fdw.Results;
using Fdw.Data.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.FixedWidth.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.FixedWidth;

/// <summary>
/// TypeOption for fixed-width (fixed-length) row writers, backed by RecordParser.
/// The write-side mirror of <see cref="FixedWidthRowSourceType"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordWriterTypes), "FixedWidth")]
public sealed class FixedWidthRowWriterType : RecordWriterTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedWidthRowWriterType"/> class.
    /// </summary>
    public FixedWidthRowWriterType() : base("FixedWidth")
    {
        BufferSize = 16 * 1024;
    }

    /// <inheritdoc />
    public override string Format => "FixedWidth";

    /// <inheritdoc />
    // Why this format is served as nothing: a fixed-width layout needs a width per field, which a
    // record schema does not carry. Advertising a media type would make it reachable over content
    // negotiation and then fail at writer construction, so it is deliberately not offered.
    public override string MediaType => string.Empty;

    /// <inheritdoc />
    public override string FileExtension => "txt";

    /// <inheritdoc />
    public override IGenericResult<RowWriterOptions> CreateOptions(IReadOnlyList<string> columns, IReadOnlyDictionary<string, object> stated, string owner)
    {
        var choices = new RowFormatChoices(stated, owner, Format);
        return choices.Result<RowWriterOptions>(new FixedWidthRowWriterOptions
        {
            Columns = [.. columns],
            WriteHeader = choices.Flag("HasHeader"),
            BufferSize = BufferSize,
        });
    }

    /// <inheritdoc />
    /// <remarks>A fixed-width file also takes each field's offset and width from the container's field metadata.</remarks>
    public override IGenericResult<RowWriterOptions> CreateOptions(IStorageContainer container)
    {
        var options = base.CreateOptions(container);
        if (options.IsSuccess && options.Value is FixedWidthRowWriterOptions fixedWidth)
            fixedWidth.Fields = FixedWidthField.FromContainer(container);
        return options;
    }

    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions options)
        => new FixedWidthStreamRowWriter(target, options as FixedWidthRowWriterOptions ?? throw new ArgumentException("The FixedWidth format writes with FixedWidthRowWriterOptions.", nameof(options)));
}
