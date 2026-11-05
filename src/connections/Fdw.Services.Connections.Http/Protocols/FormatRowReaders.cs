using System.IO;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// Finds and builds the row reader for a container's format, for every HTTP protocol that reads a response as rows.
/// </summary>
/// <remarks>
/// Why one helper (FDW-3823): the protocol base lives in a netstandard2.0 assembly that cannot reach the net10
/// <see cref="RecordSourceTypes"/> collection, so it knew only a JSON reader of its own, with the choices it read defaulted in
/// place. The REST, GraphQL and SOAP protocol bases now all read through this, by the container's format, and the format type
/// builds its options from the choices the container states, refusing a container that leaves one out.
/// </remarks>
internal static class FormatRowReaders
{
    /// <summary>Gets whether a reader is registered for the container's format.</summary>
    /// <param name="container">The container.</param>
    /// <returns>True when the format has a registered reader.</returns>
    public static bool Has(IStorageContainer container)
        => container.Format is not null && RecordSourceTypes.ByName(container.Format.Family) != RecordSourceTypes.NotFound;

    /// <summary>Creates the reader for the container's format over <paramref name="content"/>.</summary>
    /// <param name="container">The container carrying the format and the format's choices.</param>
    /// <param name="content">The response content.</param>
    /// <returns>The reader; null when no reader is registered for the format; or the failure naming a choice the container leaves out.</returns>
    public static IGenericResult<IRowSourceReader?> Create(IStorageContainer container, Stream content)
    {
        if (!Has(container))
            return GenericResult<IRowSourceReader?>.Success(null);

        var sourceType = RecordSourceTypes.ByName(container.Format.Family);
        var options = sourceType.CreateOptions(container);
        return options.IsFailure
            ? options.ToNewResult<IRowSourceReader?>()
            : GenericResult<IRowSourceReader?>.Success(sourceType.CreateReader(content, options.Value!));
    }
}
