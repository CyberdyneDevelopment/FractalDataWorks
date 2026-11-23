using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Pauses a schedule, for a threshold consequence (FDW-6144); a person enables it again.</summary>
public interface ISchedulePauser
{
    /// <summary>Pauses the schedule a rule fired on and records why.</summary>
    /// <param name="firing">The rule that matched; its subject is the schedule and its reason is recorded.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or a failure naming why the schedule could not be paused (the subject is no schedule, the schedule is not found).</returns>
    Task<IGenericResult> Pause(EscalationFiring firing, CancellationToken cancellationToken = default);
}
