using System;
using System.Collections.Generic;
using Fdw.Operations.Abstractions.Escalation;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Concrete implementation of <see cref="IEscalationPolicy"/> for use in endpoint request/response mapping.
/// </summary>
public class EscalationPolicyModel : IEscalationPolicy
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public bool IsEnabled { get; set; }

    /// <inheritdoc/>
    public Guid? WorkflowId { get; set; }

    /// <inheritdoc/>
    public int MaxEscalationLevel { get; set; }

    /// <inheritdoc/>
    public int CooldownMinutes { get; set; }

    /// <inheritdoc/>
    public IReadOnlyList<IEscalationLevel> Levels { get; set; } = [];
}
