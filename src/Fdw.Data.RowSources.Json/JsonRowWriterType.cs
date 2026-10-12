using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
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
[TypeOption(typeof(RecordWriterTypes), "Json")]
public sealed class JsonRowWriterType : RecordWriterTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRowWriterType"/> class.
    /// </summary>
    public JsonRowWriterType() : base("Json")
    {
    }

    /// <inheritdoc />
    public override string Format => "Json";

    /// <inheritdoc />
    public override string MediaType => "application/json";

    /// <inheritdoc />
    public override string FileExtension => "json";

    /// <inheritdoc />
    public override RowWriterOptions CreateOptions(IReadOnlyList<string> columns)
        => new JsonRowWriterOptions { Columns = [.. columns] };

    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions? options)
        => new JsonStreamRowWriter(target, options as JsonRowWriterOptions);
}
