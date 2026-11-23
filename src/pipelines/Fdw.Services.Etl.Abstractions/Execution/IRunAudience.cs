using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Who may follow a run besides the person it is for (FDW-6140). Sharing is the consumer's: a conversation is theirs, so a host that lets others follow it registers an audience; a host that registers none lets the
/// person alone follow.
/// </summary>
public interface IRunAudience
{
    /// <summary>Says whether a person may follow the pieces of a run.</summary>
    /// <param name="person">The person who asks.</param>
    /// <param name="tenantId">The tenant the run ran in.</param>
    /// <param name="claimRootId">The first run of the chain the pieces belong to.</param>
    /// <param name="cancellationToken">Cancels the question.</param>
    /// <returns>Whether the person may follow; or the failure that says the audience could not be read, which refuses nobody on its own and is never taken for a yes.</returns>
    Task<IGenericResult<bool>> MayFollow(Guid person, Guid tenantId, Guid claimRootId, CancellationToken cancellationToken = default);
}
