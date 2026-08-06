using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.DataReader;

/// <summary>
/// TypeOption for DataReader row sources.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "TypeOption - no logic to test")]
[TypeOption(typeof(RecordSourceTypes), "DataReader")]
public sealed class DataReaderRowSourceType : RecordSourceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataReaderRowSourceType"/> class.
    /// </summary>
    public DataReaderRowSourceType() : base(1, "DataReader")
    {
    }

    /// <inheritdoc />
    public override bool SupportsSync => true;

    /// <inheritdoc />
    public override bool SupportsAsync => false; // Standard IDataReader is sync only

    /// <inheritdoc />
    public override bool SupportsReset => false; // Forward-only

    /// <inheritdoc />
    public override int TypicalAllocationsPerRow => 0;

    /// <inheritdoc />
    public override string Format => "Tabular";

    /// <inheritdoc />
    // Why: DataReader sources wrap an EXISTING ADO.NET IDataReader, not a content stream — there is
    // no stream to parse. Fail loud rather than fabricate an empty reader (NO FALLBACKS): callers
    // that have an IDataReader construct DataReaderRowSource directly; the format-driven stream
    // dispatch only applies to text-content formats (Json/Xml/Delimited/FixedWidth).
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions? options)
        => throw new NotSupportedException(
            "DataReader row sources wrap an existing IDataReader and cannot be created from a content stream.");

    /// <inheritdoc />
    // Why: same reason as CreateReader — a DataReader row source wraps an EXISTING ADO.NET IDataReader,
    // there is no content stream to build from. Fail loud (NO FALLBACKS); callers that have an
    // IDataReader construct the row source directly. The config-driven Create surface only applies to
    // text-content formats (Json/Xml/Delimited/FixedWidth).
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => throw new NotSupportedException(
            "DataReader row sources wrap an existing IDataReader and cannot be created from a content stream.");
}
