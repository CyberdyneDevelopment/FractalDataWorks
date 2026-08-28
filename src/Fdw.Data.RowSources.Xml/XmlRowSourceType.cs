using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Xml.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Xml;

/// <summary>
/// TypeOption for XML stream row sources.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "TypeOption - no logic to test")]
[TypeOption(typeof(RecordSourceTypes), "Xml")]
public sealed class XmlRowSourceType : RecordSourceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlRowSourceType"/> class.
    /// </summary>
    public XmlRowSourceType() : base(2, "Xml")
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
    public override string Format => "Xml";

    /// <inheritdoc />
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions? options)
        => new XmlStreamRowSource(content, options as XmlRowSourceOptions);

    /// <inheritdoc />
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => CreateItemSource(context);
}
