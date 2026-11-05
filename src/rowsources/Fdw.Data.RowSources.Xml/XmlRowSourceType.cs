using Fdw.Results;
using Fdw.Data.Abstractions;
using System;
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
    public XmlRowSourceType() : base("Xml")
    {
        BufferSize = 16 * 1024;
        MaxDepth = 32;
        MaxCharactersFromEntities = 10_000_000;
    }

    /// <summary>Gets or sets the deepest element nesting the reader accepts. Declared here as this format's own value (FDW-3823); a host may change it at registration.</summary>
    public int MaxDepth { get; set; }

    /// <summary>Gets or sets the most characters the reader expands from entities. Declared here as this format's own value (FDW-3823); a host may change it at registration.</summary>
    public int MaxCharactersFromEntities { get; set; }

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
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions options)
        => new XmlStreamRowSource(content, options as XmlRowSourceOptions ?? throw new ArgumentException("The Xml format reads with XmlRowSourceOptions.", nameof(options)));

    /// <inheritdoc />
    public override IGenericResult<RowSourceOptions> CreateOptions(IStorageContainer container)
    {
        var choices = Choices(container);
        return choices.Result<RowSourceOptions>(new XmlRowSourceOptions
        {
            ContinueOnError = choices.Flag(ContinueOnErrorKey),
            UseElementContent = choices.Flag("UseElementContent"),
            IncludeAttributes = choices.Flag("IncludeAttributes"),
            RowElementName = choices.OptionalText("RowElementName"),
            RowElementPath = choices.OptionalText("RecordSelector"),
            BufferSize = BufferSize,
            MaxDepth = MaxDepth,
            MaxCharactersFromEntities = MaxCharactersFromEntities,
        });
    }

    /// <inheritdoc />
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => CreateItemSource(context);
}
