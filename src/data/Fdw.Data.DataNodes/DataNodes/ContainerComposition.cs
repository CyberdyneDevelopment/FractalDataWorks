using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// Resolves the per-container composition values (response format + response-shaping metadata)
/// that a generic <see cref="DataContainer"/> exposes to the connection's response parser.
/// </summary>
/// <remarks>
/// Why: the generic container node used to hardcode <c>Format = Tabular</c> and an empty metadata
/// bag, so <c>HttpProtocolBase.ProcessResponse</c> never saw a JSON format or a <c>RecordSelector</c>
/// for a config-built container. Format + row-shaping are now CONFIG-DRIVEN — read directly from the
/// container's own configuration (its <c>Format</c> discriminator + inline row-shaping options), NOT
/// through a separate FormatConfiguration typed-body provider domain. The record source/writer is then
/// built dynamically from that config via <c>RecordSourceTypes</c>/<c>RecordWriterTypes</c>. These
/// methods stay synchronous; there is no async format resolution and no sync-over-async.
/// </remarks>
/// <remarks>
/// Why public: shared across transport-specific builders in OTHER assemblies (e.g.
/// <c>Fdw.Services.Connections.FileSystem</c>'s <c>FileSystemDataStoreBuilder</c>) — genuinely generic
/// composition logic, not a detail private to this assembly.
/// </remarks>
public static class ContainerComposition
{
    /// <summary>
    /// Resolves the container's response format: its explicit
    /// <see cref="DataContainerConfiguration.Format"/> when set, otherwise the
    /// <paramref name="defaultResponseFormat"/> supplied by the caller. Returns
    /// <see cref="FormatTypes.NotFound"/> when neither is available — never a silent Tabular fallback.
    /// </summary>
    /// <param name="cfg">The container configuration.</param>
    /// <param name="defaultResponseFormat">
    /// The owning transport's declared default response format (e.g. Http → Json), resolved by the
    /// caller via <c>ConnectionTypes.ByName(...).DefaultResponseFormat</c> at the transport boundary.
    /// This package stays connection-agnostic and never performs that lookup itself.
    /// </param>
    public static IFormatType ResolveFormat(DataContainerConfiguration cfg, IFormatType defaultResponseFormat)
    {
        if (!string.IsNullOrWhiteSpace(cfg.Format))
            return FormatTypes.ByName(cfg.Format);

        return defaultResponseFormat;
    }

    /// <summary>
    /// Builds the <c>IStorageContainer.Metadata</c> bag that the record-source readers consume
    /// (<c>RecordSelector</c>, <c>FlattenNestedObjects</c>, <c>FlattenSeparator</c>, and for a file
    /// <c>HasHeader</c>, <c>Separator</c>, <c>SheetName</c>, and for a destination that sends over HTTP <c>Endpoint</c>,
    /// <c>Method</c>, <c>ContentType</c>), sourced directly from the inline
    /// row-shaping options on the container config — no typed-body resolution.
    /// </summary>
    /// <param name="cfg">The container configuration.</param>
    /// <param name="format">The container's resolved format (<see cref="ResolveFormat"/>).</param>
    /// <remarks>
    /// A key is present only when it is stated; the readers treat an absent key as not stated. <c>Separator</c> is stated in
    /// one place: by the format when the format defines one (<c>Csv</c>, <c>Tsv</c>), otherwise by the container. The API
    /// refuses a container separator on a format that defines its own, so the two never both exist.
    /// </remarks>
    public static IReadOnlyDictionary<string, object> BuildMetadata(DataContainerConfiguration cfg, IFormatType format)
    {
        var meta = new Dictionary<string, object>(StringComparer.Ordinal);
        Put(meta, "RecordSelector", cfg.RecordSelector);
        Put(meta, "FlattenNestedObjects", cfg.FlattenNestedObjects);
        Put(meta, "FlattenSeparator", cfg.FlattenSeparator);
        Put(meta, "HasHeader", cfg.HasHeader);
        // A format that defines its separator (Csv, Tsv) states it; only the plain Delimited format takes the container's.
        Put(meta, "Separator", string.IsNullOrEmpty(format.Separator) ? cfg.Separator : format.Separator);
        Put(meta, "SheetName", cfg.SheetName);
        // The format choices a container states (FDW-3823): a reader or writer refuses a container that leaves out one its format needs.
        Put(meta, nameof(cfg.ContinueOnError), cfg.ContinueOnError);
        Put(meta, nameof(cfg.PropertyNameCaseInsensitive), cfg.PropertyNameCaseInsensitive);
        Put(meta, nameof(cfg.SkipValidation), cfg.SkipValidation);
        Put(meta, nameof(cfg.UseElementContent), cfg.UseElementContent);
        Put(meta, nameof(cfg.IncludeAttributes), cfg.IncludeAttributes);
        Put(meta, nameof(cfg.OmitXmlDeclaration), cfg.OmitXmlDeclaration);
        Put(meta, nameof(cfg.ContainsQuotedFields), cfg.ContainsQuotedFields);
        Put(meta, nameof(cfg.Trim), cfg.Trim);
        Put(meta, nameof(cfg.WriteByteOrderMark), cfg.WriteByteOrderMark);
        Put(meta, nameof(cfg.NeutralizeFormulas), cfg.NeutralizeFormulas);
        Put(meta, nameof(cfg.RootElementName), cfg.RootElementName);
        Put(meta, nameof(cfg.RowElementName), cfg.RowElementName);
        Put(meta, nameof(cfg.QuoteChar), cfg.QuoteChar);
        Put(meta, "Endpoint", cfg.WriteEndpoint);
        Put(meta, "Method", cfg.WriteMethod);
        Put(meta, "ContentType", cfg.WriteContentType);
        return meta;
    }

    // A flag the container does not state is left out of the metadata, so the reader or writer that needs it says it is missing.
    private static void Put(Dictionary<string, object> meta, string key, bool? value)
    {
        if (value.HasValue)
            meta[key] = value.Value;
    }

    // Text is stated when it is not empty; whitespace is text (a tab is a separator).
    private static void Put(Dictionary<string, object> meta, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            meta[key] = value!;
    }
}
