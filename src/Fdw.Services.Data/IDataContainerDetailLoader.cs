using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Lazy loader for the field and key detail of a single DataContainer.
/// Implementations query the typed-body tables (<c>data.MsSqlDataContainerField</c>, etc.)
/// joined to the parent header tables on first access. Results are cached per container.
/// </summary>
/// <remarks>
/// The interface is storage-type-agnostic. Concrete implementations dispatch on the
/// <c>typeId</c> parameter to reach the appropriate typed-body table family
/// (MsSql, JsonFile, XmlFile, Rest, etc.). Only the MsSql family is implemented
/// initially; other families return empty and log a warning until they ship.
/// </remarks>
public interface IDataContainerDetailLoader
{
    /// <summary>
    /// Loads the fields for a container, joining the typed-body table for storage-specific metadata.
    /// Returns a cached result on subsequent calls for the same <paramref name="containerRowId"/>.
    /// </summary>
    /// <param name="containerRowId">The physical RowId of the <c>data.DataContainer</c> header row.</param>
    /// <param name="typeId">
    /// The container type discriminator (e.g., "MsSqlTable", "MsSqlView", "MsSqlSproc").
    /// Used to select the appropriate typed-body table.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<IDataField>> LoadFields(Guid containerRowId, string typeId, CancellationToken ct);

    /// <summary>
    /// Loads the keys for a container, joining the typed-body table for storage-specific metadata.
    /// Returns a cached result on subsequent calls for the same <paramref name="containerRowId"/>.
    /// </summary>
    /// <param name="containerRowId">The physical RowId of the <c>data.DataContainer</c> header row.</param>
    /// <param name="typeId">
    /// The container type discriminator (e.g., "MsSqlTable", "MsSqlView").
    /// Used to select the appropriate typed-body table.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<IContainerKey>> LoadKeys(Guid containerRowId, string typeId, CancellationToken ct);
}
