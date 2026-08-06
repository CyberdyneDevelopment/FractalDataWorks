using System;
using System.Collections.Generic;
using System.IO;
using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The configuration passed to a record-source factory (<c>RecordSourceTypes.ByName(format).Create(...)</c>)
/// to build a reader DYNAMICALLY from a container's configuration — the content to read, the container's
/// field children (the flyweight schema), and the format-specific options. There is no compile-time
/// binding to a format: the format name selects the factory; this context supplies everything that
/// factory needs.
/// </summary>
/// <remarks>
/// Why this exists: the resolved design says the generic container carries its format + format-options
/// as configuration and its record source is created from that config — no per-format container class.
/// This context is the shape that config takes when handed to the factory. <see cref="Fields"/> are the
/// container's <see cref="IDataField"/> children; the factory turns them into the shared
/// <see cref="RecordSchema"/> flyweight for every record the source yields.
/// </remarks>
public sealed class RecordSourceContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordSourceContext"/> class.
    /// </summary>
    /// <param name="content">The input stream containing the data in the selected format.</param>
    /// <param name="fields">The container's field children — the flyweight schema for produced records.</param>
    /// <param name="options">
    /// Format-specific options (the concrete subclass of <see cref="RowSourceOptions"/> for the selected
    /// format). Null requests the format's defaults.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> or <paramref name="fields"/> is null.</exception>
    public RecordSourceContext(Stream content, IReadOnlyList<IDataField> fields, RowSourceOptions? options = null)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        Options = options;
    }

    /// <summary>
    /// Gets the input stream containing the data in the selected format.
    /// </summary>
    public Stream Content { get; }

    /// <summary>
    /// Gets the container's field children — the schema described once and shared (flyweight) across
    /// every record the built source yields.
    /// </summary>
    public IReadOnlyList<IDataField> Fields { get; }

    /// <summary>
    /// Gets the format-specific options, or null to request the format's defaults.
    /// </summary>
    public RowSourceOptions? Options { get; }
}
