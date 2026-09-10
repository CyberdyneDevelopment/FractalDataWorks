using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Polymorphic identity row for one named key on a DataContainer (Primary, Foreign, Surrogate,
/// Natural, Unique). Maps to <c>data.DataContainerKey</c>.
/// </summary>
/// <remarks>
/// Storage-specific key shape (constraint name, clustering, fill factor, etc.) lives on the typed
/// body — <c>data.MsSqlDataContainerKey</c>, joined by <c>RowId</c>. The participating fields
/// live in <c>data.DataContainerKeyField</c> rows that point back via <c>DataContainerKeyRowId</c>.
/// FK→PK linking is at the key level via <see cref="ReferencedKeyId"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataContainerKey")]
public sealed partial class DataContainerKeyConfiguration : IGenericConfiguration, IDataContainerKeyImplementationConfiguration
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;


    /// <summary>Durable logical identity of this key.</summary>
    public Guid Id { get; set; }
    /// <summary>Configuration name (key name) for IGenericConfiguration contract.</summary>
    public string Name { get; set; } = string.Empty;


    /// <summary>Logical Id of the owning DataContainer.</summary>
    public Guid DataContainerId { get; set; }


    /// <summary>
    /// KeyType discriminator string (e.g., "PrimaryKey", "Foreign", "Surrogate", "Natural", "Unique").
    /// Resolved at runtime via <c>KeyTypes.ByName(TypeId)</c>.
    /// </summary>
    public string TypeId { get; set; } = string.Empty;

    /// <summary>
    /// Logical Id of the referenced key (FK→PK linking at the key level).
    /// <see langword="null"/> for non-referencing keys (PrimaryKey, Surrogate, Natural).
    /// </summary>
    public Guid? ReferencedKeyId { get; set; }

    /// <summary>Optional description of this key's purpose.</summary>
    public string? Description { get; set; }

    /// <summary>Whether this is the current active version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// JSON-bound: name of the referenced container (resolved to <see cref="ReferencedKeyId"/> at startup).
    /// </summary>
    [NotMapped]
    public string? ReferencedContainerName { get; set; }

    /// <summary>
    /// JSON-bound: name of the referenced key on the referenced container (e.g. "PK_logical").
    /// </summary>
    [NotMapped]
    public string? ReferencedKeyName { get; set; }

    /// <summary>
    /// JSON-bound: nested key fields. The flat <c>data.DataContainerKeyField</c> rows map onto this
    /// list when binding from the configuration JSON. Loader fills the Guid Id columns at startup.
    /// </summary>
    [NotMapped]
#pragma warning disable MA0016 // collection abstraction — required for IOptions binding
    public List<DataContainerKeyFieldConfiguration> KeyFields { get; set; } = [];
#pragma warning restore MA0016
}
