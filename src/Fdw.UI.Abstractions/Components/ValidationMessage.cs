using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Abstractions.Components;

/// <summary>
/// Represents a single validation message.
/// </summary>
// Why: pure DTO, no logic.
[ExcludeFromCodeCoverage]
public sealed class ValidationMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationMessage"/> class.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="severity">The message severity.</param>
    /// <param name="propertyName">The property name this message relates to.</param>
    public ValidationMessage(string message, IValidationSeverity severity, string? propertyName = null)
    {
        Message = message;
        Severity = severity;
        PropertyName = propertyName;
    }

    /// <summary>
    /// Gets the message text.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the message severity.
    /// </summary>
    public IValidationSeverity Severity { get; }

    /// <summary>
    /// Gets the property name this message relates to.
    /// </summary>
    public string? PropertyName { get; }
}
