using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>Base for result codes raised by the descriptions domain.</summary>
[ExcludeFromCodeCoverage]
public abstract class DescriptionsResultCodeBase : ResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionsResultCodeBase"/> class.</summary>
    protected DescriptionsResultCodeBase()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DescriptionsResultCodeBase"/> class.</summary>
    /// <param name="number">The catalog number, whose leading digit is the category.</param>
    /// <param name="name">The code name.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether retrying could succeed.</param>
    protected DescriptionsResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        // messageTemplate BEFORE prefix: ResultCodeBase takes (number, name, severity, messageTemplate, prefix, isRetryable).
        : base(number, name, severity, messageTemplate, "Descriptions", isRetryable)
    {
    }
}
