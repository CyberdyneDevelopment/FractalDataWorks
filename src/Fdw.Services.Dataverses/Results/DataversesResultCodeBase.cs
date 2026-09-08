using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Dataverses.Results;

/// <summary>Base for result codes raised by the dataverses domain.</summary>
[ExcludeFromCodeCoverage]
public abstract class DataversesResultCodeBase : ResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataversesResultCodeBase"/> class.</summary>
    protected DataversesResultCodeBase()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DataversesResultCodeBase"/> class.</summary>
    /// <param name="number">The catalogue number, whose leading digit is the category.</param>
    /// <param name="name">The code name.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether retrying could succeed.</param>
    protected DataversesResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        // messageTemplate BEFORE prefix. ResultCodeBase takes (number, name, severity,
        // messageTemplate, prefix, isRetryable) and builds Code = $"{prefix}-{number}",
        // Domain = prefix, MessageTemplate = messageTemplate. Reversed, every dataverse error
        // reported its whole message template as its code ("Dataverse '{name}' rejected: ...
        // -20001") and carried the literal string "Dataverses" as its message, so the real text
        // never reached the client and its placeholders had nothing to substitute into.
        // Both parameters are string and adjacent, so the swap compiled and stayed silent.
        : base(number, name, severity, messageTemplate, "Dataverses", isRetryable)
    {
    }
}
