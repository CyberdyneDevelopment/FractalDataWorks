using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Configuration class for dataset field definitions.
/// </summary>
// Why: implements IGenericConfiguration so [GenerateMapper] emits a CascadeChildren descriptor for the
// parent DataSetConfiguration.Fields collection — the keystone base read (ComposeChildren) then loads
// data.DataSetField rows, matched via the child's ConfigurationCommand.ContainerName ("DataSetField").
// IsKey/IsIndexed/DefaultValue/IsJoinKey/CalculationName all have DataSetField columns and round-trip
// through the save translator's column-intersection. Only Calculator (a runtime delegate) has no column —
// it cannot be persisted, so the translator's column-intersection excludes it and reads default it
// (unchanged from how AssembleHierarchy read this type) — no [NotMapped] needed (the generator ignores it).
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class DataFieldConfiguration : IGenericConfiguration
{
    /// <summary>Gets the configuration section name (computed; not a persisted column).</summary>
    public string SectionName => "DataSetFields";

    /// <summary>Gets the service type domain.</summary>
    public string ServiceType => "DataSet";

    /// <summary>Gets the service option type discriminator (none for fields).</summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the stable logical identifier for this field record.
    /// App-minted (Guid.CreateVersion7()) before INSERT; never DB-generated.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the field.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the .NET type name of the field.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property role (Surrogate, NaturalKey, Lookup, Attribute, Measure).
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this field is part of the primary key.
    /// This is derived from Role being Surrogate or NaturalKey.
    /// </summary>
    public bool IsKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this field is required (non-nullable).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this field can be used for indexing/searching.
    /// </summary>
    public bool IsIndexed { get; set; }

    /// <summary>
    /// Gets or sets the maximum length for string fields, or null if not applicable.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the default value for this field as a string representation.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the parent DataSet identifier (FK to data.DataSet.Id).
    /// </summary>
    public Guid DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the ordinal position of this field within the DataSet.
    /// </summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current active version of the record.
    /// </summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this record has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the original creation date from the source system (if migrated).
    /// </summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>
    /// Gets the timestamp when the record was created in this system.
    /// </summary>
    public DateTimeOffset CreateDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the database user who created the record.
    /// </summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets the application user on whose behalf the record was created.
    /// </summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the record was last modified.
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the database user who last modified the record.
    /// </summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application user on whose behalf the record was last modified.
    /// </summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this field participates as a join key for cross-source joins.
    /// </summary>
    public bool IsJoinKey { get; set; }

    /// <summary>
    /// Gets or sets the name of the configured calculation that computes this field's value.
    /// When set, the DataGateway resolves the named calculation at query time.
    /// </summary>
    public string? CalculationName { get; set; }

    /// <summary>
    /// Gets or sets the calculator function for computed fields.
    /// When set, this field's value is calculated from other fields in the row.
    /// </summary>
    public Func<IDataRow, object>? Calculator { get; set; }

    /// <summary>
    /// Gets whether this field is calculated (either via a runtime Calculator or a named CalculationName).
    /// </summary>
    public bool IsCalculated => Calculator != null || CalculationName != null;

    /// <summary>
    /// Creates a clone of this field configuration.
    /// </summary>
    /// <returns>A cloned instance of the field configuration.</returns>
    public DataFieldConfiguration Clone()
    {
        return new DataFieldConfiguration
        {
            Id = Id,
            Name = Name,
            Description = Description,
            TypeName = TypeName,
            Role = Role,
            IsKey = IsKey,
            IsRequired = IsRequired,
            IsIndexed = IsIndexed,
            MaxLength = MaxLength,
            DefaultValue = DefaultValue,
            DataSetId = DataSetId,
            Ordinal = Ordinal,
            IsCurrent = IsCurrent,
            IsDeleted = IsDeleted,
            SrcCreateDate = SrcCreateDate,
            CreateDate = CreateDate,
            CreateBy = CreateBy,
            CreateOnBehalfOf = CreateOnBehalfOf,
            ModifyDate = ModifyDate,
            ModifyBy = ModifyBy,
            ModifyOnBehalfOf = ModifyOnBehalfOf,
            IsJoinKey = IsJoinKey,
            CalculationName = CalculationName,
            Calculator = Calculator
        };
    }
}