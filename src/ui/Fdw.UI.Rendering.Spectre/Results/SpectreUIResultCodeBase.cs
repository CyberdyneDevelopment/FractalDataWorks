using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.UI.Rendering.Spectre.Results;

/// <summary>
/// Base class for Spectre UI result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class SpectreUIResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected SpectreUIResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreUIResultCodeBase"/> class.
    /// </summary>
    protected SpectreUIResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "SpectreUI", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreUIResultCodeBase"/> class from a categorized number.
    /// </summary>
    protected SpectreUIResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SPECTRE", isRetryable)
    {
    }
}