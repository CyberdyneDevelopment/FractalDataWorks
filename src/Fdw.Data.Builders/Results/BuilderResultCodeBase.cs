using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Base class for Data.Builders result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class BuilderResultCodeBase : ResultCodeBase, IBuilderResultCode
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected BuilderResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuilderResultCodeBase"/> class.
    /// </summary>
    protected BuilderResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Data.Builders", messageTemplate, isRetryable) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuilderResultCodeBase"/> class using a categorized number.
    /// </summary>
    protected BuilderResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "BUILDER", isRetryable) { }
}