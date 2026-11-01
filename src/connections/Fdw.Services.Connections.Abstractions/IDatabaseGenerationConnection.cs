using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Optional connection capability: say which generation of its database the connection reaches.
/// </summary>
/// <remarks>
/// <para>
/// A generation is an opaque text that stays the same while the database behind the connection is the same one and
/// changes when that database is dropped and created again. It lets a host tell that a database it has read from was
/// reset underneath it (the database seed pipeline does this, and so does a person with a script), which nothing
/// else in the host would notice: the cached reads keep answering with rows that are no longer there until they expire.
/// </para>
/// <para>
/// Why a separate capability interface and not a member of <c>IDataConnection</c>: the abstraction targets
/// <c>netstandard2.0</c>, which has no default interface methods, so a member there would force every connection type
/// to implement it. A connection of a kind that cannot tell (a file system, an HTTP service) implements nothing, and
/// the caller feature-detects it, as it does <see cref="IRecordSourceConnection"/>.
/// </para>
/// </remarks>
public interface IDatabaseGenerationConnection
{
    /// <summary>
    /// Reads the generation of the database this connection reaches.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The generation text, or a failure when the database could not be reached. A database that is not there has no
    /// generation, so that is a failure and not an empty text.
    /// </returns>
    Task<IGenericResult<string>> DatabaseGeneration(CancellationToken cancellationToken = default);
}
