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
    }

    /// <inheritdoc />
    public override string Format => "Delimited";

    /// <inheritdoc />
    public override string MediaType => "text/csv";

    /// <inheritdoc />
    public override string FileExtension => "csv";

    /// <inheritdoc />
    public override RowWriterOptions CreateOptions(IReadOnlyList<string> columns)
        // Why the header is written on this path specifically: these options are built FROM a column
        // list, so the column names are known and named. A delimited payload whose columns are known
        // to the producer and unlabelled to the consumer is strictly less useful. The container-driven
        // Create(RecordWriterContext) path is untouched and keeps its own WriteHeader setting.
        => new DelimitedRowWriterOptions { Columns = [.. columns], WriteHeader = true };

    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions? options)
        => new DelimitedStreamRowWriter(target, options as DelimitedRowWriterOptions);
}
