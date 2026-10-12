using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Calculations.Abstractions.ResultCodes;

/// <summary>
/// Base class for calculation entity result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class CalculationEntityResultCodeBase : ResultCodeBase, ICalculationEntityResultCode
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected CalculationEntityResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationEntityResultCodeBase"/> class.
    /// </summary>
    protected CalculationEntityResultCodeBase(
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
    /// Initializes a new instance of the <see cref="CalculationEntityResultCodeBase"/> class
    /// with a categorized number identity.
    /// </summary>
    protected CalculationEntityResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "CALC", isRetryable)
    {
    }
}
