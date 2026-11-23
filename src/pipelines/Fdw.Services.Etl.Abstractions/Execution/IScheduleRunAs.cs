using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Says who a schedule's fire acts as (FDW-6144). The person comes only from the grant the schedule stores: nothing a fire relays can name one. A schedule with no grant acts as no person.
/// </summary>
/// <remarks>
/// Every refusal is a coded message of its own: the schedule or its grant is not found, the fire is not for the pipeline and tenant the schedule has, the grant has ended or was revoked, the
/// grant allows less than the run's roles carry, or the person may no longer act (the judgment a run that starts uses, <c>RUNPERSON</c>).
/// </remarks>
public interface IScheduleRunAs
{
    /// <summary>Resolves who a fire acts as.</summary>
    /// <param name="fire">The fire.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    /// <returns>The person, or none for a schedule without a grant; or the refusal.</returns>
    Task<IGenericResult<ScheduledRunPerson>> Resolve(ScheduledFire fire, CancellationToken cancellationToken = default);
}
