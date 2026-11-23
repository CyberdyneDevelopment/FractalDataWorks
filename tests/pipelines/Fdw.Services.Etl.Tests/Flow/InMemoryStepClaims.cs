using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// The claim store the way its table behaves (FDW-6139): one claim for each tenant, root, component and iteration, a second claim of the same answers the first with its outcome, and an outcome is written
/// onto the claim. It states every answer; nothing is assumed.
/// </summary>
internal sealed class InMemoryStepClaims : IPipelineStepClaims
{
    private readonly object _gate = new();
    private readonly Dictionary<(Guid Tenant, Guid Root, string Component, int Iteration), Row> _rows = [];

    /// <summary>Gets or sets whether recording an outcome fails.</summary>
    public bool RecordFails { get; set; }

    /// <summary>Gets the keys of the claims made, in the order made.</summary>
    public List<(Guid Root, string Component, int Iteration, Guid Execution)> Made { get; } = [];

    /// <summary>Gets the outcome recorded for a claim, or null when none was.</summary>
    public string? OutcomeOf(Guid root, string component, int iteration)
    {
        lock (_gate)
        {
            return _rows.Where(pair => pair.Key.Root == root && pair.Key.Component == component && pair.Key.Iteration == iteration).Select(pair => pair.Value.Outcome).FirstOrDefault();
        }
    }

    /// <summary>Puts a claim in the store as an earlier run left it.</summary>
    public void Seed(Guid tenant, Guid root, string component, int iteration, Guid execution, string? outcome)
    {
        lock (_gate)
        {
            _rows[(tenant, root, component, iteration)] = new Row(Guid.NewGuid(), execution, outcome);
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult<StepClaim>> Claim(Guid tenantId, Guid claimRootId, string component, int iteration, Guid executionId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_rows.TryGetValue((tenantId, claimRootId, component, iteration), out var found))
            {
                return Task.FromResult(GenericResult<StepClaim>.Success(new StepClaim(found.Id, false, found.Execution, found.Outcome)));
            }

            var row = new Row(Guid.NewGuid(), executionId, null);
            _rows[(tenantId, claimRootId, component, iteration)] = row;
            Made.Add((claimRootId, component, iteration, executionId));
            return Task.FromResult(GenericResult<StepClaim>.Success(new StepClaim(row.Id, true, executionId, null)));
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult> Record(Guid tenantId, Guid claimId, string outcome, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (RecordFails)
            {
                return Task.FromResult<IGenericResult>(GenericResult.Failure(new Fdw.Messages.GenericMessage("the store is down")));
            }

            foreach (var key in _rows.Keys.ToList())
            {
                if (_rows[key].Id == claimId)
                {
                    _rows[key] = _rows[key] with { Outcome = outcome };
                }
            }

            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }
    }

    private sealed record Row(Guid Id, Guid Execution, string? Outcome);
}
