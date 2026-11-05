using Fdw.Results;
using Fdw.Data.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.FixedWidth.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.FixedWidth;

/// <summary>
/// TypeOption for fixed-width (fixed-length) stream row sources, backed by RecordParser.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordSourceTypes), "FixedWidth")]
public sealed class FixedWidthRowSourceType : RecordSourceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedWidthRowSourceType"/> class.
    /// </summary>
    public FixedWidthRowSourceType() : base("FixedWidth")
    {
        BufferSize = 16 * 1024;
    }

    /// <inheritdoc />
    public override bool SupportsSync => true;

    /// <inheritdoc />
    public override bool SupportsAsync => false;

    /// <inheritdoc />
    public override bool SupportsReset => false;

    /// <inheritdoc />
    public override int TypicalAllocationsPerRow => 1;

    /// <inheritdoc />
    public override string Format => "FixedWidth";

    /// <inheritdoc />
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions options)
        => new FixedWidthStreamRowSource(content, options as FixedWidthRowSourceOptions ?? throw new ArgumentException("The FixedWidth format reads with FixedWidthRowSourceOptions.", nameof(options)));

    /// <inheritdoc />
    public override IGenericResult<RowSourceOptions> CreateOptions(IStorageContainer container)
    {
        var choices = Choices(container);
        // Why: the offsets and widths come from per-field metadata; a container that states none gets an empty list, and the reader
        // refuses it rather than guessing widths.
        return choices.Result<RowSourceOptions>(new FixedWidthRowSourceOptions
        {
            ContinueOnError = choices.Flag(ContinueOnErrorKey),
            HasHeader = choices.Flag("HasHeader"),
            Trim = choices.Flag("Trim"),
            Fields = FixedWidthField.FromContainer(container),
            BufferSize = BufferSize,
        });
    }

    /// <inheritdoc />
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => CreateRowSource(context);
}
