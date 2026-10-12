using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>Base for result codes raised by the connections domain.</summary>
[ExcludeFromCodeCoverage]
public abstract class ConnectionsResultCodeBase : ResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionsResultCodeBase"/> class.</summary>
    protected ConnectionsResultCodeBase()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ConnectionsResultCodeBase"/> class.</summary>
    /// <param name="number">The catalogue number, whose leading digit is the category.</param>
    /// <param name="name">The code name.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether retrying could succeed.</param>
    protected ConnectionsResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        // messageTemplate BEFORE prefix: ResultCodeBase takes (number, name, severity, messageTemplate, prefix,
        // isRetryable). Both are strings and adjacent, so a swap compiles and silently carries the template as the code.
        : base(number, name, severity, messageTemplate, "Connections", isRetryable)
    {
    }
}
