using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Universes.Results;

/// <summary>Base for result codes raised by the universes domain.</summary>
[ExcludeFromCodeCoverage]
public abstract class UniversesResultCodeBase : ResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UniversesResultCodeBase"/> class.</summary>
    protected UniversesResultCodeBase()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="UniversesResultCodeBase"/> class.</summary>
    /// <param name="number">The catalogue number, whose leading digit is the category.</param>
    /// <param name="name">The code name.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether retrying could succeed.</param>
    protected UniversesResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, "Universes", messageTemplate, isRetryable)
    {
    }
}
