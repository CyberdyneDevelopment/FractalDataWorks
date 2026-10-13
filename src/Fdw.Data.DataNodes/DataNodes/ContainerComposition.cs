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
    /// <c>HasHeader</c>, <c>Separator</c>, <c>SheetName</c>), sourced directly from the inline
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
        ArgumentNullException.ThrowIfNull(format);

        var meta = new Dictionary<string, object>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(cfg.RecordSelector))
            meta["RecordSelector"] = cfg.RecordSelector!;
        if (cfg.FlattenNestedObjects.HasValue)
            meta["FlattenNestedObjects"] = cfg.FlattenNestedObjects.Value;
        if (!string.IsNullOrWhiteSpace(cfg.FlattenSeparator))
            meta["FlattenSeparator"] = cfg.FlattenSeparator!;
        if (cfg.HasHeader.HasValue)
            meta["HasHeader"] = cfg.HasHeader.Value;
        if (!string.IsNullOrEmpty(format.Separator))
            meta["Separator"] = format.Separator!;
        else if (!string.IsNullOrEmpty(cfg.Separator))
            meta["Separator"] = cfg.Separator!;
        if (!string.IsNullOrWhiteSpace(cfg.SheetName))
            meta["SheetName"] = cfg.SheetName!;

        return meta;
    }
}
