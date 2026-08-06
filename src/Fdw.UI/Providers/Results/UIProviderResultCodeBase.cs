using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.UI.Providers.Results;

/// <summary>
/// Base class for UI provider-context result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class UIProviderResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UIProviderResultCodeBase"/> class.
    /// </summary>
    protected UIProviderResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UIProviderResultCodeBase"/> class.
    /// </summary>
    /// <param name="number">The categorized code number.</param>
    /// <param name="name">The code name.</param>
    /// <param name="severity">The result severity.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether the operation is retryable.</param>
    protected UIProviderResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "UIPROVIDER", isRetryable)
    {
    }
}
