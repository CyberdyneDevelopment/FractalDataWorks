using Fdw.Results;
using Fdw.Data.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Xml.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Xml;

/// <summary>
/// TypeOption for XML row writers. The write-side mirror of <see cref="XmlRowSourceType"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordWriterTypes), "Xml")]
public sealed class XmlRowWriterType : RecordWriterTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlRowWriterType"/> class.
    /// </summary>
    public XmlRowWriterType() : base("Xml")
    {
        BufferSize = 16 * 1024;
        Indent = false;
    }

    /// <summary>Gets or sets whether the writer indents its output. Declared here as this format's own value (FDW-3823); a host may change it at registration.</summary>
    public bool Indent { get; set; }

    /// <inheritdoc />
    public override string Format => "Xml";

    /// <inheritdoc />
    public override string MediaType => "application/xml";

    /// <inheritdoc />
    public override string FileExtension => "xml";

    /// <inheritdoc />
    public override IGenericResult<RowWriterOptions> CreateOptions(IReadOnlyList<string> columns, IReadOnlyDictionary<string, object> stated, string owner)
    {
        var choices = new RowFormatChoices(stated, owner, Format);
        return choices.Result<RowWriterOptions>(new XmlRowWriterOptions
        {
            Columns = [.. columns],
            RootElementName = choices.Text("RootElementName"),
            RowElementName = choices.Text("RowElementName"),
            Indent = Indent,
            OmitXmlDeclaration = choices.Flag("OmitXmlDeclaration"),
            BufferSize = BufferSize,
        });
    }


    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions options)
        => new XmlStreamRowWriter(target, options as XmlRowWriterOptions ?? throw new ArgumentException("The Xml format writes with XmlRowWriterOptions.", nameof(options)));
}
