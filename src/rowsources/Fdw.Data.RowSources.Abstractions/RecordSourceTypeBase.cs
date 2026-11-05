using System.IO;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Base class for record source types using the CRTP pattern. The factory that builds a reader from a
/// container's configuration. (Renamed from <c>RowSourceTypeBase</c>: it builds RECORD sources — items
/// or rows.)
/// </summary>
public abstract class RecordSourceTypeBase : TypeOptionBase<RecordSourceTypeBase>, IRecordSourceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordSourceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this type.</param>
    protected RecordSourceTypeBase(string name) : base(name)
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
    /// <remarks>False unless the format overrides it: the text formats (Json, Xml, Delimited, FixedWidth) are read as text.</remarks>
    public virtual bool ReadsBinary => false;

    /// <inheritdoc />
    public abstract IRowSourceReader CreateReader(Stream content, RowSourceOptions options);

    /// <inheritdoc />
    /// <remarks>Set by each format's constructor, the declaration of its own value (FDW-3823); zero for a format that reads no stream.</remarks>
    public int BufferSize { get; set; }

    /// <inheritdoc />
    /// <remarks>The base options, for a format with no options of its own; a format that has some overrides it.</remarks>
    public virtual IGenericResult<RowSourceOptions> CreateOptions(IStorageContainer container)
    {
        var choices = Choices(container);
        return choices.Result(new RowSourceOptions { ContinueOnError = choices.Flag(ContinueOnErrorKey), BufferSize = BufferSize });
    }

    /// <summary>The metadata key a container states whether its reader goes on after a row it cannot read with.</summary>
    protected const string ContinueOnErrorKey = "ContinueOnError";

    /// <summary>Opens a reader over the choices <paramref name="container"/> states.</summary>
    /// <param name="container">The container.</param>
    /// <returns>The reader, naming the container and this format in a failure.</returns>
    protected RowFormatChoices Choices(IStorageContainer container)
        => container is null ? throw new System.ArgumentNullException(nameof(container)) : new RowFormatChoices(container.Metadata, container.Name, Format);

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
