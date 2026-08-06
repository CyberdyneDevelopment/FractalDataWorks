using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Delimited;

/// <summary>
/// TypeOption for delimited (CSV / variable-length) stream row sources, backed by RecordParser.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordSourceTypes), "Delimited")]
public sealed class DelimitedRowSourceType : RecordSourceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedRowSourceType"/> class.
    /// </summary>
    public DelimitedRowSourceType() : base(6, "Delimited")
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
    public override string Format => "Delimited";

    /// <inheritdoc />
    // Why: the format-driven read seam. Downcast to DelimitedRowSourceOptions when supplied; a base
    // RowSourceOptions (or null) yields the reader's defaults — but column names are required, so the
    // reader fails loud when none are configured (NO FALLBACKS).
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions? options)
        => new DelimitedStreamRowSource(content, options as DelimitedRowSourceOptions);

    /// <inheritdoc />
    // Why: delimited is ROW-oriented — build a RowCursorRecordSource (an IRowSource) so consumers get
    // both DataRecord enumeration and ordinal cursor access over the container's field schema.
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => CreateRowSource(context);
}
