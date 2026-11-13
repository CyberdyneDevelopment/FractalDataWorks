using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Options.Results;

/// <summary>
/// Base class for data option result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class DataOptionResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected DataOptionResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized number (Code == "DATAOPTION-{number}").
    /// </summary>
    /// <param name="number">The categorized number.</param>
    /// <param name="name">The code's name.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="messageTemplate">The message, with <c>{Name}</c> placeholders for the details.</param>
    /// <param name="isRetryable">Whether trying again can succeed.</param>
    protected DataOptionResultCodeBase(int number, string name, IResultSeverity severity, string messageTemplate, bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "DATAOPTION", isRetryable)
    {
    }
}
