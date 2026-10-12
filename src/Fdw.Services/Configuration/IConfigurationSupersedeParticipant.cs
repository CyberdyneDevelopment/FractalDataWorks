using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Configuration;

/// <summary>
/// An implementation provider that can retire its rows without deleting them, inside a unit of work
/// the domain provider that owns them opened.
/// </summary>
/// <typeparam name="TContract">The domain's implementation contract.</typeparam>
/// <remarks>
/// Why this is separate from <see cref="IConfigurationWriteParticipant{TContract}"/>: a new version of a
/// domain row has a new RowId, and the implementation's rows hang from the RowId, so writing the
/// implementation under the new row retires nothing of what hangs from the old one. The old
/// implementation is superseded by the old row's keys, before the row itself is. Only the domain
/// provider's save of a changed record needs this, so a provider that never takes part in one need not
/// carry it -- and the test doubles that implement the write contract do not have to grow a member.
/// </remarks>
internal interface IConfigurationSupersedeParticipant<TContract> : IConfigurationWriteParticipant<TContract>
    where TContract : IImplementationConfiguration
{
    /// <summary>Retires this implementation's row and its children, leaving them in the row's history, inside the caller's unit of work.</summary>
    /// <param name="domain">The domain container the row hangs from.</param>
    /// <param name="domainKeyValues">The key values of the domain row version being replaced; the rows retired are those whose Foreign key to <paramref name="domain"/> holds them.</param>
    /// <param name="unit">The unit of work the domain provider opened.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the failure that stopped it.</returns>
    Task<IGenericResult> Supersede(
        IDataContainer domain,
        IReadOnlyDictionary<string, object?> domainKeyValues,
        ConfigurationWriteScope unit,
        CancellationToken cancellationToken);
}
