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
    public FixedWidthRowSourceType() : base(7, "FixedWidth")
    {
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
    // Why: the format-driven read seam. Downcast to FixedWidthRowSourceOptions when supplied; field
    // definitions are required, so the reader fails loud when none are configured (NO FALLBACKS).
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions? options)
        => new FixedWidthStreamRowSource(content, options as FixedWidthRowSourceOptions);

    /// <inheritdoc />
    // Why: fixed-width is ROW-oriented — build a RowCursorRecordSource (an IRowSource) over the
    // container's field schema.
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => CreateRowSource(context);
}
