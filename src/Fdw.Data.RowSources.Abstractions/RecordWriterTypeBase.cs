using System.IO;
using Fdw.Collections;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Base class for record writer types using the CRTP pattern. The write-side mirror of
/// <see cref="RecordSourceTypeBase"/>. (Renamed from <c>RowWriterTypeBase</c>: it builds RECORD writers —
/// items or rows.)
/// </summary>
public abstract class RecordWriterTypeBase : TypeOptionBase<int, RecordWriterTypeBase>, IRecordWriterType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordWriterTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this type.</param>
    /// <param name="name">The name of this type.</param>
    protected RecordWriterTypeBase(int id, string name) : base(id, name)
    {
    }

    /// <inheritdoc />
    public abstract string Format { get; }

    /// <inheritdoc />
    public abstract IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions? options);

    /// <inheritdoc />
    // Why: the config-driven surface delegates to CreateWriter with the context's target + options; the
    // field children flow into the writer's column order via the options the caller built from the
    // container (delimited/fixed-width read column order from RowWriterOptions.Columns). Item writers
    // (Json/Xml) ignore column order. No second writer path.
    public virtual IRecordWriter<DataRecord> Create(RecordWriterContext context)
        => CreateWriter(context.Target, context.Options);
}
