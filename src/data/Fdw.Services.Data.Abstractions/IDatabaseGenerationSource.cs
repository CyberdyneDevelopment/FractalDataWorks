using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Something that can say which generation of its database it reads, so a host can tell the database was reset.
/// </summary>
/// <remarks>
/// Implemented by the configuration gateway, which reaches its database through a connection and asks that connection.
/// A separate interface and not a member of <see cref="IConfigurationGateway"/>: every hand-written gateway stand-in
/// would then have to implement it, and a gateway that cannot tell is a normal case, answered with
/// <see cref="DatabaseGenerationReading.CannotTell"/>.
/// </remarks>
public interface IDatabaseGenerationSource
{
    /// <summary>Gets the configuration connection this source reads.</summary>
    string ConnectionName { get; }

    /// <summary>Asks which generation of its database this source reads now.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The reading (a generation, or that the connection cannot tell), or a failure when the database could not be
    /// reached.
    /// </returns>
    Task<IGenericResult<DatabaseGenerationReading>> DatabaseGeneration(CancellationToken cancellationToken = default);
}
