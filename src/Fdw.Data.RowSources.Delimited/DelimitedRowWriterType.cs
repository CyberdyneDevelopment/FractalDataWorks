using System.Diagnostics.CodeAnalysis;
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
[TypeOption(typeof(RecordWriterTypes), "Delimited", RestrictToCurrentCompilation = true)]
public sealed class DelimitedRowWriterType : RecordWriterTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedRowWriterType"/> class.
    /// </summary>
    public DelimitedRowWriterType() : base(6, "Delimited")
    {
    }

    /// <inheritdoc />
    public override string Format => "Delimited";

    /// <inheritdoc />
    // Why: delimited is a ROW writer — DelimitedStreamRowWriter implements IRowWriter (itself an
    // IRecordWriter<DataRecord>). The return type matches the base abstract signature exactly.
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions? options)
        => new DelimitedStreamRowWriter(target, options as DelimitedRowWriterOptions);
}
