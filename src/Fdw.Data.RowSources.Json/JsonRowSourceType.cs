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
    // Why: the format-driven read seam. Downcast to JsonRowSourceOptions when supplied; a base
    // RowSourceOptions (or null) yields JsonStreamRowSource's own defaults — never a guessed format.
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions? options)
        => new JsonStreamRowSource(content, options as JsonRowSourceOptions);

    /// <inheritdoc />
    // Why: JSON builds a RowCursorRecordSource (via the shared CreateRowSource helper — the same one
    // Delimited/FixedWidth use), exposing IRowSource.Cursor over the underlying JsonStreamRowSource. This
    // is NOT "JSON is now genuinely tabular" — JsonStreamRowSource's Read() still yields a DataRecord
    // schema-projected onto the container's declared fields (via CursorRecordSource.Project(), shared by
    // both helpers) for the normal record-enumeration path. Exposing the cursor additionally lets a caller
    // that needs the FULL row (every property actually present in that JSON object, not just the declared
    // ones) read it directly from JsonStreamRowSource — which, unlike Delimited/FixedWidth (whose
    // Columns/Fields options are built FROM the declared schema, so their cursor's field set always equals
    // it), tracks whatever properties the source JSON object actually carries. FileSystemRecordConnector.Read
    // uses this to build a schema-superset row dictionary for the version-on-write config read/write path,
    // preventing a column beyond the declared schema from being silently dropped on rewrite.
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => CreateRowSource(context);
}
