using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>Base for result codes raised by the tags domain.</summary>
[ExcludeFromCodeCoverage]
public abstract class TagsResultCodeBase : ResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagsResultCodeBase"/> class.</summary>
    protected TagsResultCodeBase()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="TagsResultCodeBase"/> class.</summary>
    /// <param name="number">The catalogue number, whose leading digit is the category.</param>
    /// <param name="name">The code name.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether retrying could succeed.</param>
    protected TagsResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        // messageTemplate BEFORE prefix: ResultCodeBase takes (number, name, severity, messageTemplate, prefix, isRetryable).
        : base(number, name, severity, messageTemplate, "Tags", isRetryable)
    {
    }
}
