using System.Collections.Generic;
using System.IO;
using Fdw.Collections;

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
    public abstract RowWriterOptions CreateOptions(IReadOnlyList<string> columns);

    /// <inheritdoc />
    public abstract IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions? options);

    /// <inheritdoc />
    public virtual IRecordWriter<DataRecord> Create(RecordWriterContext context)
        => CreateWriter(context.Target, context.Options);
}
