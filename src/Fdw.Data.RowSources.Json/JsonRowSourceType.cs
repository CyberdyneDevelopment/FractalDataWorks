using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Json;

/// <summary>
/// TypeOption for JSON stream row sources.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "TypeOption - no logic to test")]
[TypeOption(typeof(RecordSourceTypes), "Json")]
public sealed class JsonRowSourceType : RecordSourceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRowSourceType"/> class.
    /// </summary>
    public JsonRowSourceType() : base(4, "Json")
    {
    }

    /// <inheritdoc />
    public override bool SupportsSync => true;

    /// <inheritdoc />
    public override bool SupportsAsync => true;

    /// <inheritdoc />
    public override bool SupportsReset => false;

    /// <inheritdoc />
    public override int TypicalAllocationsPerRow => 1;

    /// <inheritdoc />
    public override string Format => "Json";

    /// <inheritdoc />
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions? options)
        => new JsonStreamRowSource(content, options as JsonRowSourceOptions);

    /// <inheritdoc />
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => CreateRowSource(context);
}
