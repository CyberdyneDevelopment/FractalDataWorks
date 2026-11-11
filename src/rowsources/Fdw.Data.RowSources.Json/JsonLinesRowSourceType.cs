using Fdw.Results;
using Fdw.Data.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Json;

/// <summary>
/// TypeOption for JSON Lines row sources: each line of the file is one JSON record (FDW-4971).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "TypeOption - no logic to test")]
[TypeOption(typeof(RecordSourceTypes), "JsonLines")]
public sealed class JsonLinesRowSourceType : RecordSourceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonLinesRowSourceType"/> class.
    /// </summary>
    public JsonLinesRowSourceType() : base("JsonLines")
    {
        BufferSize = 16 * 1024;
        MaxDepth = 64;
        MaxStringLength = 128 * 1024 * 1024;
        AllowTrailingCommas = true;
        AllowComments = false;
    }

    /// <summary>Gets or sets the deepest nesting the reader accepts. Declared here as this format's own value (FDW-3823); a host may change it at registration.</summary>
    public int MaxDepth { get; set; }

    /// <summary>Gets or sets the longest string, in bytes, the reader accepts. Declared here as this format's own value (FDW-3823); a host may change it at registration.</summary>
    public int MaxStringLength { get; set; }

    /// <summary>Gets or sets whether the reader accepts trailing commas. Declared here as this format's own value (FDW-3823); a host may change it at registration.</summary>
    public bool AllowTrailingCommas { get; set; }

    /// <summary>Gets or sets whether the reader accepts comments. Declared here as this format's own value (FDW-3823); a host may change it at registration.</summary>
    public bool AllowComments { get; set; }

    /// <inheritdoc />
    /// <remarks>Reads one line's record at a time through Utf8JsonReader as the stream is read (FDW-4971).</remarks>
    public override bool ParsesIncrementally => true;

    /// <inheritdoc />
    public override bool SupportsSync => true;

    /// <inheritdoc />
    public override bool SupportsAsync => true;

    /// <inheritdoc />
    public override bool SupportsReset => false;

    /// <inheritdoc />
    public override int TypicalAllocationsPerRow => 1;

    /// <inheritdoc />
    public override string Format => "JsonLines";

    /// <inheritdoc />
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions options)
        => new JsonStreamRowSource(content, options as JsonRowSourceOptions ?? throw new ArgumentException("The Json format reads with JsonRowSourceOptions.", nameof(options)));

    /// <inheritdoc />
    public override IGenericResult<RowSourceOptions> CreateOptions(IStorageContainer container)
    {
        var choices = Choices(container);
        var flatten = choices.Flag("FlattenNestedObjects");
        return choices.Result<RowSourceOptions>(new JsonRowSourceOptions
        {
            ContinueOnError = choices.Flag(ContinueOnErrorKey),
            AllowTrailingCommas = AllowTrailingCommas,
            AllowComments = AllowComments,
            PropertyNameCaseInsensitive = choices.Flag("PropertyNameCaseInsensitive"),
            FlattenNestedObjects = flatten,
            // The separator is a choice only where nested objects are flattened; a container that does not flatten names none.
            FlattenSeparator = flatten ? choices.Text("FlattenSeparator") : string.Empty,
            // Why no records path: each line is a record, so there is no array to find.
            RowArrayPath = null,
            RecordsAreLines = true,
            BufferSize = BufferSize,
            MaxDepth = MaxDepth,
            MaxStringLength = MaxStringLength,
        });
    }

    /// <inheritdoc />
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => CreateRowSource(context);
}
