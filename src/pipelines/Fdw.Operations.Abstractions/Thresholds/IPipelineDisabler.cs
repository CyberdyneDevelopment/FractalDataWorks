using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Disables a pipeline, for a threshold consequence (FDW-3936); a person enables it again.</summary>
public interface IPipelineDisabler
{
    /// <summary>Disables a pipeline and records why.</summary>
    /// <param name="firing">The rule that matched; its subject names the pipeline and its reason is recorded.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or a failure naming why the pipeline could not be disabled.</returns>
    Task<IGenericResult> Disable(EscalationFiring firing, CancellationToken cancellationToken = default);
}
