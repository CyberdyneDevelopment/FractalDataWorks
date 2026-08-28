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
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions? options)
        => new DelimitedStreamRowSource(content, options as DelimitedRowSourceOptions);

    /// <inheritdoc />
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => CreateRowSource(context);
}
