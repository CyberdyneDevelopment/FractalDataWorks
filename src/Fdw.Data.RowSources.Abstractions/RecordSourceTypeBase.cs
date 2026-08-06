using System.IO;
using Fdw.Collections;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Base class for record source types using the CRTP pattern. The factory that builds a reader from a
/// container's configuration. (Renamed from <c>RowSourceTypeBase</c>: it builds RECORD sources — items
/// or rows.)
/// </summary>
public abstract class RecordSourceTypeBase : TypeOptionBase<int, RecordSourceTypeBase>, IRecordSourceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordSourceTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this type.</param>
    /// <param name="name">The name of this type.</param>
    protected RecordSourceTypeBase(int id, string name) : base(id, name)
    {
    }

    /// <inheritdoc />
    public abstract bool SupportsSync { get; }

    /// <inheritdoc />
    public abstract bool SupportsAsync { get; }

    /// <inheritdoc />
    public abstract bool SupportsReset { get; }

    /// <inheritdoc />
    public abstract int TypicalAllocationsPerRow { get; }

    /// <inheritdoc />
    public abstract string Format { get; }

    /// <inheritdoc />
    public abstract IRowSourceReader CreateReader(Stream content, RowSourceOptions? options);

    /// <inheritdoc />
    public abstract IRecordSource<DataRecord> Create(RecordSourceContext context);

    /// <summary>
    /// Builds a ROW source (Delimited/FixedWidth/Tabular) from the context: creates the cursor via
    /// <see cref="CreateReader"/> and wraps it in a <see cref="RowCursorRecordSource"/> so callers get
    /// both record enumeration and ordinal cursor access. Format types whose <see cref="Create"/> is
    /// row-oriented call this.
    /// </summary>
    /// <param name="context">The container configuration.</param>
    /// <param name="logger">Optional logger threaded to the created source for its construction-time diagnostic.</param>
    /// <returns>A row source over the content.</returns>
    protected IRowSource CreateRowSource(RecordSourceContext context, ILogger? logger = null)
        => new RowCursorRecordSource(CreateReader(context.Content, context.Options), context.Fields, logger);

    /// <summary>
    /// Builds an ITEM source (Json/Xml) from the context: creates the cursor via <see cref="CreateReader"/>
    /// and wraps it in a <see cref="CursorRecordSource"/> (item source — does NOT expose a cursor). Format
    /// types whose <see cref="Create"/> yields items call this.
    /// </summary>
    /// <param name="context">The container configuration.</param>
    /// <param name="logger">Optional logger threaded to the created source for its construction-time diagnostic.</param>
    /// <returns>An item record source over the content.</returns>
    protected IRecordSource<DataRecord> CreateItemSource(RecordSourceContext context, ILogger? logger = null)
        => new CursorRecordSource(CreateReader(context.Content, context.Options), context.Fields, logger);
}
