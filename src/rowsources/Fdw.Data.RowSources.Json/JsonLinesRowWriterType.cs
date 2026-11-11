using Fdw.Results;
using Fdw.Data.Abstractions;
using System;
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
[TypeOption(typeof(RecordWriterTypes), "JsonLines")]
public sealed class JsonLinesRowWriterType : RecordWriterTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonLinesRowWriterType"/> class.
    /// </summary>
    public JsonLinesRowWriterType() : base("JsonLines")
    {
        BufferSize = 16 * 1024;
    }

    /// <inheritdoc />
    public override string Format => "JsonLines";

    /// <inheritdoc />
    public override string MediaType => "application/jsonl";

    /// <inheritdoc />
    public override string FileExtension => "jsonl";

    /// <inheritdoc />
    public override IGenericResult<RowWriterOptions> CreateOptions(IReadOnlyList<string> columns, IReadOnlyDictionary<string, object> stated, string owner)
    {
        var choices = new RowFormatChoices(stated, owner, Format);
        return choices.Result<RowWriterOptions>(new JsonRowWriterOptions
        {
            Columns = [.. columns],
            // Why never indented: a JSON Lines record is one line.
            WriteIndented = false,
            SkipValidation = choices.Flag("SkipValidation"),
            RecordsAreLines = true,
            BufferSize = BufferSize,
        });
    }


    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions options)
        => new JsonStreamRowWriter(target, options as JsonRowWriterOptions ?? throw new ArgumentException("The Json format writes with JsonRowWriterOptions.", nameof(options)));
}
