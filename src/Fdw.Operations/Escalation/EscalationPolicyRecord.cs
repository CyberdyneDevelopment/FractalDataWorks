using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Configuration;

namespace Fdw.Operations.Escalation;

/// <summary>
/// Internal adapter mapping <see cref="EscalationPolicyConfiguration"/> to <see cref="IEscalationPolicy"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class EscalationPolicyRecord : IEscalationPolicy
{
    private readonly EscalationPolicyConfiguration _config;

    internal EscalationPolicyRecord(EscalationPolicyConfiguration config)
    {
        _config = config;
    }

    public Guid Id => _config.Id;
    public string Name => _config.Name;
    public bool IsEnabled => _config.IsEnabled;
    public Guid? WorkflowId => _config.WorkflowId;
    public int MaxEscalationLevel => _config.MaxEscalationLevel;
    public int CooldownMinutes => _config.CooldownMinutes;

    public IReadOnlyList<IEscalationLevel> Levels =>
        (_config.Levels ?? [])
            .OrderBy(l => l.Level)
            .Select(l => (IEscalationLevel)new EscalationLevelRecord(l))
            .ToList();
}
