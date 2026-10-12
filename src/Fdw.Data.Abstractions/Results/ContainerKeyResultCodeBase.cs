using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// Base class for ContainerKey result codes — declaring, resolving, and writing a
/// <see cref="Fdw.Data.Abstractions.IContainerKey"/> (Primary/Foreign/Natural/Surrogate/Join) on a
/// DataContainer. Lives in Fdw.Data.Abstractions alongside <see cref="Fdw.Data.Abstractions.IContainerKey"/>
/// and <see cref="Fdw.Data.Abstractions.KeyTypeBase"/> so every key-declaring package can reference it.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ContainerKeyResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ContainerKeyResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized number (Code == "CONTAINERKEY-{number}").
    /// </summary>
    protected ContainerKeyResultCodeBase(int number, string name, IResultSeverity severity, string messageTemplate, bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "CONTAINERKEY", isRetryable)
    {
    }
}
