using System.Diagnostics.CodeAnalysis;
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
[TypeOption(typeof(RecordWriterTypes), "Xml", RestrictToCurrentCompilation = true)]
public sealed class XmlRowWriterType : RecordWriterTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlRowWriterType"/> class.
    /// </summary>
    public XmlRowWriterType() : base(2, "Xml")
    {
    }

    /// <inheritdoc />
    public override string Format => "Xml";

    /// <inheritdoc />
    public override IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions? options)
        => new XmlStreamRowWriter(target, options as XmlRowWriterOptions);
}
