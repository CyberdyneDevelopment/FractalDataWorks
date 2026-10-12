using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Expressions.Results;

/// <summary>
/// Base class for Expression result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ExpressionResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ExpressionResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionResultCodeBase"/> class.
    /// </summary>
    protected ExpressionResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Expression", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized <paramref name="number"/> (catalog scheme).
    /// </summary>
    protected ExpressionResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "EXPR", isRetryable)
    {
    }
}