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
[TypeOption(typeof(RecordWriterTypes), "Json")]
public sealed class JsonRowWriterType : RecordWriterTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRowWriterType"/> class.
    /// </summary>
    public JsonRowWriterType() : base("Json")
    {
        BufferSize = 16 * 1024;
        WriteIndented = false;
    }

    /// <summary>Gets or sets whether the writer indents its output. Declared here as this format's own value (FDW-3823); a host may change it at registration.</summary>
    public bool WriteIndented { get; set; }

    /// <inheritdoc />
    public override string Format => "Json";

    /// <inheritdoc />
    public override string MediaType => "application/json";

    /// <inheritdoc />
    public override string FileExtension => "json";

    /// <inheritdoc />
    public override IGenericResult<RowWriterOptions> CreateOptions(IReadOnlyList<string> columns, IReadOnlyDictionary<string, object> stated, string owner)
    {
        var choices = new RowFormatChoices(stated, owner, Format);
        return choices.Result<RowWriterOptions>(new JsonRowWriterOptions
        {
            Columns = [.. columns],
            WriteIndented = WriteIndented,
            SkipValidation = choices.Flag("SkipValidation"),
            RecordsAreLines = false,
            BufferSize = BufferSize,
        });
    }


    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions options)
        => new JsonStreamRowWriter(target, options as JsonRowWriterOptions ?? throw new ArgumentException("The Json format writes with JsonRowWriterOptions.", nameof(options)));
}
