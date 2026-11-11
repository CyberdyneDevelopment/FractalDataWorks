using System;
using System.Collections.Generic;
using System.IO;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Base class for record writer types using the CRTP pattern. The write-side mirror of
/// <see cref="RecordSourceTypeBase"/>. (Renamed from <c>RowWriterTypeBase</c>: it builds RECORD writers —
/// items or rows.)
/// </summary>
public abstract class RecordWriterTypeBase : TypeOptionBase<RecordWriterTypeBase>, IRecordWriterType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordWriterTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this type.</param>
    protected RecordWriterTypeBase(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public abstract string Format { get; }

    /// <inheritdoc />
    public abstract string MediaType { get; }

    /// <inheritdoc />
    public abstract string FileExtension { get; }

    /// <inheritdoc />
    /// <remarks>Set by each format's constructor, the declaration of its own value (FDW-3823).</remarks>
    public int BufferSize { get; set; }

    /// <inheritdoc />
    public abstract IGenericResult<RowWriterOptions> CreateOptions(IReadOnlyList<string> columns, IReadOnlyDictionary<string, object> stated, string owner);

    /// <inheritdoc />
    /// <remarks>The container's metadata states the choices and its fields are the columns; a format that needs more of the container
    /// (fixed-width reads each field's offset and width) overrides it.</remarks>
    public virtual IGenericResult<RowWriterOptions> CreateOptions(IStorageContainer container)
        => container is null
            ? throw new ArgumentNullException(nameof(container))
            : CreateOptions(ContainerRowMetadata.FieldNames(container), container.Metadata, container.Name);

    /// <inheritdoc />
    public abstract IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions options);

    /// <inheritdoc />
    /// <remarks>False unless the format overrides it: the text formats (Json, Xml, Delimited, FixedWidth) are written as text.</remarks>
    public virtual bool WritesBinary => false;

    /// <inheritdoc />
    /// <remarks>Refuses by default, naming the format: a text format asked for a byte writer is a caller's mistake, not a thing to paper over.</remarks>
    public virtual IRecordWriter<DataRecord> CreateBinaryWriter(Stream target, RowWriterOptions options)
        => throw new NotSupportedException($"The {Format} format is written as text; it has no byte-stream writer.");

    /// <inheritdoc />
    /// <remarks>
    /// The one way a connection builds a writer for any format, chosen by the container's format name: a text format writes to
    /// the context's text target, a binary one to its byte target. A context of the other kind is refused, naming the format.
    /// </remarks>
    public virtual IRecordWriter<DataRecord> Create(RecordWriterContext context)
    {
        if (WritesBinary)
        {
            return CreateBinaryWriter(
                context.ByteTarget ?? throw new NotSupportedException($"The {Format} format is written to a byte stream; build its context with RecordWriterContext.ForBytes."),
                context.Options);
        }

        return context.ByteTarget is null
            ? CreateWriter(context.Target, context.Options)
            : throw new NotSupportedException($"The {Format} format is written as text; it cannot write to a byte stream.");
    }
}
