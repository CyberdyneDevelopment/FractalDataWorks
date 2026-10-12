using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Calculations.Results;

/// <summary>
/// Base class for Calculation result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class CalculationResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected CalculationResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationResultCodeBase"/> class.
    /// </summary>
    protected CalculationResultCodeBase(
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
    /// Initializes a new instance of the <see cref="CalculationResultCodeBase"/> class from a categorized number.
    /// </summary>
    protected CalculationResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "CALC", isRetryable)
    {
    }
}