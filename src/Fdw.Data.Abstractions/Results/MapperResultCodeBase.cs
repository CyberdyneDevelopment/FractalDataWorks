using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// Base class for POCO-mapper result codes (emitted by the PocoMapperGenerator). Lives in
/// Fdw.Data.Abstractions so every generated mapper can reference it.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class MapperResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected MapperResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized number (Code == "MAPPER-{number}").
    /// </summary>
    protected MapperResultCodeBase(int number, string name, IResultSeverity severity, string messageTemplate, bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "MAPPER", isRetryable)
    {
    }
}
