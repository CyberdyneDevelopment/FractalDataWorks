using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Messaging;

/// <summary>
/// Says which actions a kind of alert offers the people of a tenant (FDW-2430): the ones the tenant chose, in the order it chose them, or the kind's own list
/// when it chose none.
/// </summary>
internal sealed class AlertKindChoices
{
    private readonly AlertStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlertKindChoices"/> class.
    /// </summary>
    /// <param name="store">Reads what each tenant chose.</param>
    public AlertKindChoices(AlertStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>Reads the actions a kind offers each tenant that is asked about.</summary>
    /// <param name="source">The kind of alert.</param>
    /// <param name="tenantIds">The tenants.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>For each tenant, the names of the actions the kind offers its people, in order.</returns>
    public async Task<IGenericResult<Dictionary<Guid, List<string>>>> For(IAlertSource source, IEnumerable<Guid> tenantIds, CancellationToken cancellationToken)
    {
        var offered = new Dictionary<Guid, List<string>>();
        foreach (var tenantId in tenantIds)
        {
            if (offered.ContainsKey(tenantId))
                continue;

            var chosen = await _store.FindKindSettings(tenantId, cancellationToken).ConfigureAwait(false);
            if (!chosen.IsSuccess)
                return chosen.ToNewResult<Dictionary<Guid, List<string>>>();

            // Why the kind's own list when the tenant chose none: the default the kind states in code is the configuration a tenant has not yet changed; a tenant that
            // chose nothing for the kind has a setting with an empty list, which is a choice and not an absence.
            offered[tenantId] = chosen.Value!.TryGetValue(source.Kind, out var list) ? list : [.. source.OfferedActions];
        }

        return GenericResult<Dictionary<Guid, List<string>>>.Success(offered);
    }
}
