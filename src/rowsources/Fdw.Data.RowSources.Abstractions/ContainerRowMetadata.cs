using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Reads a container's field names, for the format types that build their options from a container.
/// </summary>
/// <remarks>
/// Why one helper: the HTTP protocols, the HTTP record connector and the file-system connector each read a container's fields with
/// their own copy of this code (FDW-3823). The format types now build the options, and the choices they read go through
/// <see cref="RowFormatChoices"/>.
/// </remarks>
public static class ContainerRowMetadata
{
    /// <summary>Gets the container's field names, in the order the schema declares them.</summary>
    /// <param name="container">The container.</param>
    /// <returns>The names; none when the container declares no fields.</returns>
    public static List<string> FieldNames(IStorageContainer container)
        => container.Schema?.Fields is { } fields ? [.. fields.Select(field => field.Name)] : [];
}
