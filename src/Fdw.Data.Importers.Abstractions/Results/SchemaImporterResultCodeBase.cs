using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.SchemaImporters.Abstractions.Results;

/// <summary>
/// Base class for Schema Importer result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class SchemaImporterResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected SchemaImporterResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaImporterResultCodeBase"/> class.
    /// </summary>
    protected SchemaImporterResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "SchemaImporter", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized <paramref name="number"/> — the catalog scheme
    /// where the number is the whole identity (Id == EventId == number, Code == "SCHEMA-{number}").
    /// </summary>
    protected SchemaImporterResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SCHEMA", isRetryable)
    {
    }
}