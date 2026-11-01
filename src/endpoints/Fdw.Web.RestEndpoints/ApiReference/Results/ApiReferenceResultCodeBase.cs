using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Web.RestEndpoints.ApiReference.Results;

/// <summary>
/// Base class for the API reference's result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ApiReferenceResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ApiReferenceResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiReferenceResultCodeBase"/> class
    /// using a categorized number as the code identity.
    /// </summary>
    /// <param name="number">The categorized number; its category is the number divided by 10000.</param>
    /// <param name="name">The code's name.</param>
    /// <param name="severity">How severe the result is; Error and above are failures.</param>
    /// <param name="messageTemplate">The message, with placeholders the result details fill.</param>
    /// <param name="isRetryable">Whether repeating the operation may succeed.</param>
    protected ApiReferenceResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "APIREF", isRetryable)
    {
    }
}
