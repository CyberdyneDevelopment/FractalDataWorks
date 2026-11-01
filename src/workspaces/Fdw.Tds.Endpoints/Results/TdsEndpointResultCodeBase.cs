using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Tds.Endpoints.Results;

/// <summary>Base for result codes raised by the SQL Server endpoint.</summary>
[ExcludeFromCodeCoverage]
public abstract class TdsEndpointResultCodeBase : ResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TdsEndpointResultCodeBase"/> class.</summary>
    protected TdsEndpointResultCodeBase()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="TdsEndpointResultCodeBase"/> class.</summary>
    /// <param name="number">The catalogue number, whose leading digit is the category.</param>
    /// <param name="name">The code name.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether retrying could succeed.</param>
    protected TdsEndpointResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        // messageTemplate before the prefix: ResultCodeBase takes (number, name, severity, messageTemplate,
        // prefix, isRetryable), and the two strings swapped would still compile.
        : base(number, name, severity, messageTemplate, "SQLEP", isRetryable)
    {
    }
}
