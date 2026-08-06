using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// Base class for DataStores domain result codes (e.g. <see cref="Fdw.Data.DataStores.Abstractions.DataLocation"/>
/// canonical-string addressing). Lives in Fdw.Data.Abstractions so every DataStores type can
/// reference it without a back-reference to an implementation project.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class DataStoresResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected DataStoresResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized number (Code == "DATASTORE-{number}").
    /// </summary>
    protected DataStoresResultCodeBase(int number, string name, IResultSeverity severity, string messageTemplate, bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "DATASTORE", isRetryable)
    {
    }
}
