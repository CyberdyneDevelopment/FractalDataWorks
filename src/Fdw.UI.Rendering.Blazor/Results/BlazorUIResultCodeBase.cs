using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.UI.Rendering.Blazor.Results;

/// <summary>
/// Base class for Blazor UI rendering result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class BlazorUIResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorUIResultCodeBase"/> class.
    /// </summary>
    protected BlazorUIResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorUIResultCodeBase"/> class.
    /// </summary>
    /// <param name="number">The categorized code number.</param>
    /// <param name="name">The code name.</param>
    /// <param name="severity">The result severity.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether the operation is retryable.</param>
    protected BlazorUIResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "BLAZORUI", isRetryable)
    {
    }
}
