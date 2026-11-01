using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A step that does something when the run fails, besides what it does when the sequence reaches it (a notification set to fire on failure).
/// </summary>
public interface IFlowFailureHook : IFlowStep
{
    /// <summary>Does the step's work for a run that failed.</summary>
    /// <param name="context">What the run carries.</param>
    /// <param name="failure">The failure that ended the run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the work was done; otherwise why it was not.</returns>
    Task<IGenericResult> OnFailure(FlowStepContext context, IGenericResult failure, CancellationToken cancellationToken = default);
}
