using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Disables the connection until a person enables it again.</summary>
[TypeOption(typeof(EscalationTypes), "DisableConnection")]
public sealed class DisableConnectionEscalationType : EscalationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DisableConnectionEscalationType"/> class.</summary>
    public DisableConnectionEscalationType() : base("DisableConnection", "Disables the connection until a person enables it again.", needsPolicy: false, needsNotificationRules: false, actsOnRun: false)
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult> Handle(EscalationFiring firing, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (firing is null)
        {
            throw new ArgumentNullException(nameof(firing));
        }
        if (EscalationServices.Find<IConnectionDisabler>(services) is not { } disabler)
        {
            return EscalationFailures.ServiceMissing(firing, Name, nameof(IConnectionDisabler));
        }

        return await disabler.Disable(firing, cancellationToken).ConfigureAwait(false);
    }
}
