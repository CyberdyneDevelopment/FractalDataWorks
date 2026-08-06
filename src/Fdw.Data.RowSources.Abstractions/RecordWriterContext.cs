using System;
using System.Collections.Generic;
using System.IO;
using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The configuration passed to a record-writer factory (<c>RecordWriterTypes.ByName(format).Create(...)</c>)
/// to build a writer DYNAMICALLY from a container's configuration — the destination, the container's
/// field children (the flyweight schema / column order), and the format-specific options. The write-side
/// mirror of <see cref="RecordSourceContext"/>.
/// </summary>
/// <remarks>
/// Why this exists: a writer's column/field schema must come from the container config, never a
/// compile-time POCO. <see cref="Fields"/> are the container's <see cref="IDataField"/> children; the
/// factory turns them into the shared <see cref="RecordSchema"/> flyweight and (for row formats) the
/// stable column order.
/// </remarks>
public sealed class RecordWriterContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordWriterContext"/> class.
    /// </summary>
    /// <param name="target">The destination text writer.</param>
    /// <param name="fields">The container's field children — the flyweight schema / column order.</param>
    /// <param name="options">
    /// Format-specific options (the concrete subclass of <see cref="RowWriterOptions"/> for the selected
    /// format). Null requests the format's defaults.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> or <paramref name="fields"/> is null.</exception>
    public RecordWriterContext(TextWriter target, IReadOnlyList<IDataField> fields, RowWriterOptions? options = null)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        Options = options;
    }

    /// <summary>
    /// Gets the destination text writer.
    /// </summary>
    public TextWriter Target { get; }

    /// <summary>
    /// Gets the container's field children — the schema described once and shared (flyweight) across
    /// every record the built writer accepts, and the stable column order for row formats.
    /// </summary>
    public IReadOnlyList<IDataField> Fields { get; }

    /// <summary>
    /// Gets the format-specific options, or null to request the format's defaults.
    /// </summary>
    public RowWriterOptions? Options { get; }
}
