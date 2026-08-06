using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.StateCollections.Results;

/// <summary>Base class for state-machine result codes.</summary>
[ExcludeFromCodeCoverage]
public abstract class StateMachineResultCodeBase : ResultCodeBase
{
    /// <summary>Initializes the Empty sentinel.</summary>
    protected StateMachineResultCodeBase()
    {
    }

    /// <summary>Initializes a new state-machine result code.</summary>
    protected StateMachineResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "StateMachine", messageTemplate, isRetryable)
    {
    }

    /// <summary>Initializes a new state-machine result code from a categorized number.</summary>
    protected StateMachineResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SM", isRetryable)
    {
    }
}
