using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Json;

/// <summary>
/// TypeOption for JSON row writers. The write-side mirror of <see cref="JsonRowSourceType"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordWriterTypes), "Json", RestrictToCurrentCompilation = true)]
public sealed class JsonRowWriterType : RecordWriterTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRowWriterType"/> class.
    /// </summary>
    public JsonRowWriterType() : base(4, "Json")
    {
    }

    /// <inheritdoc />
    public override string Format => "Json";

    /// <inheritdoc />
    // Why: JSON is an ITEM writer — JsonStreamRowWriter implements IRecordWriter<DataRecord> (not
    // IRowWriter). The base Create() delegates here, so the config-driven surface reuses this.
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions? options)
        => new JsonStreamRowWriter(target, options as JsonRowWriterOptions);
}
