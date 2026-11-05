using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Disables the pipeline until a person enables it again; a run in flight finishes.</summary>
[TypeOption(typeof(EscalationTypes), "DisablePipeline")]
public sealed class DisablePipelineEscalationType : EscalationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DisablePipelineEscalationType"/> class.</summary>
    public DisablePipelineEscalationType() : base("DisablePipeline", "Disables the pipeline until a person enables it again; a run in flight finishes.", needsPolicy: false, needsNotificationRules: false, actsOnRun: false)
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult> Handle(EscalationFiring firing, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (firing is null)
        {
            throw new ArgumentNullException(nameof(firing));
        }
        if (EscalationServices.Find<IPipelineDisabler>(services) is not { } disabler)
        {
            return EscalationFailures.ServiceMissing(firing, Name, nameof(IPipelineDisabler));
        }

        return await disabler.Disable(firing, cancellationToken).ConfigureAwait(false);
    }
}
