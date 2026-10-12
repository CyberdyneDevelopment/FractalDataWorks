using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// Base class for the result codes of the calculation tree.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class CalculationTreeResultCodeBase : ResultCodeBase, ICalculationTreeResultCode
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected CalculationTreeResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationTreeResultCodeBase"/> class.
    /// </summary>
    protected CalculationTreeResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Calculations", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationTreeResultCodeBase"/> class
    /// with a categorized number identity.
    /// </summary>
    protected CalculationTreeResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "CALC", isRetryable)
    {
    }
}
