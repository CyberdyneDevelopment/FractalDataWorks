using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A consequence a rule can fire (FDW-3936).</summary>
public interface IEscalationType : ITypeOption<int, IEscalationType>
{
    /// <summary>Gets what the consequence does, in a sentence (the description every type option states).</summary>
    string Description { get; }

    /// <summary>Gets whether a rule with this consequence must name an escalation policy.</summary>
    bool NeedsPolicy { get; }

    /// <summary>Gets whether a rule with this consequence must name notification rules.</summary>
    bool NeedsNotificationRules { get; }

    /// <summary>Gets whether the consequence acts on the run the values were measured in, so the subject must give its run.</summary>
    bool ActsOnRun { get; }

    /// <summary>Does what the consequence does.</summary>
    /// <param name="firing">The rule that matched and what it matched.</param>
    /// <param name="services">The host services; a handler resolves the one service it acts through and fails loud when the host registered none.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or a failure naming why the consequence could not act.</returns>
    Task<IGenericResult> Handle(EscalationFiring firing, IServiceProvider services, CancellationToken cancellationToken = default);
}
