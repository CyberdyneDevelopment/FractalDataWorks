using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>
/// Base class for the options of <see cref="EscalationTypes"/> (FDW-3936), using the CRTP pattern. An option is a consequence and its handler.
/// </summary>
public abstract class EscalationTypeBase : TypeOptionBase<EscalationTypeBase>, IEscalationType
{
    /// <summary>Initializes a new instance of the <see cref="EscalationTypeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What the consequence does, as the console shows it.</param>
    /// <param name="needsPolicy">Whether a rule must name an escalation policy.</param>
    /// <param name="needsNotificationRules">Whether a rule must name notification rules.</param>
    /// <param name="actsOnRun">Whether it acts on the run the values were measured in.</param>
    protected EscalationTypeBase(string name, string description, bool needsPolicy, bool needsNotificationRules, bool actsOnRun)
        : base(name, name, description, "EscalationTypes")
    {
        NeedsPolicy = needsPolicy;
        NeedsNotificationRules = needsNotificationRules;
        ActsOnRun = actsOnRun;
    }

    /// <inheritdoc/>
    public bool NeedsPolicy { get; }

    /// <inheritdoc/>
    public bool NeedsNotificationRules { get; }

    /// <inheritdoc/>
    public bool ActsOnRun { get; }

    /// <inheritdoc/>
    public abstract Task<IGenericResult> Handle(EscalationFiring firing, IServiceProvider services, CancellationToken cancellationToken = default);
}
