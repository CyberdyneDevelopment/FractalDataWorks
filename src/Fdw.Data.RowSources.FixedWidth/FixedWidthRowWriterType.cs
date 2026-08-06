using System.Diagnostics.CodeAnalysis;
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
[TypeOption(typeof(RecordWriterTypes), "FixedWidth", RestrictToCurrentCompilation = true)]
public sealed class FixedWidthRowWriterType : RecordWriterTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedWidthRowWriterType"/> class.
    /// </summary>
    public FixedWidthRowWriterType() : base(7, "FixedWidth")
    {
    }

    /// <inheritdoc />
    public override string Format => "FixedWidth";

    /// <inheritdoc />
    // Why: fixed-width is a ROW writer — FixedWidthStreamRowWriter implements IRowWriter (itself an
    // IRecordWriter<DataRecord>).
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions? options)
        => new FixedWidthStreamRowWriter(target, options as FixedWidthRowWriterOptions);
}
