using System;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Configuration for a key field within a DataSet.
/// Loaded from <c>data.DataSetKeyField</c> as a child of DataSetFieldConfiguration.
/// </summary>
/// <remarks>
/// Why: Mirrors DataContainerKeyField — the table cites fields by Id (DataSetFieldId,
/// ReferencedFieldId), which a field keeps across the data set's saves; a RowId does not survive one.
/// </remarks>
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataSetKeyField")]
public partial class DataSetKeyFieldConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name (mirrors <see cref="KeyName"/>; not a persisted column).</summary>
    public string Name { get; set; } = string.Empty;



    /// <summary>Gets or sets the key/constraint name.</summary>
    public string KeyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the key type: Surrogate, Natural, or Foreign.</summary>
    public string KeyType { get; set; } = string.Empty;

    /// <summary>Gets or sets the ordinal position within the key.</summary>
    public int Ordinal { get; set; }


    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    /// <remarks>
    /// Why: The data.DataSetKeyField DDL table carries IsCurrent/IsDeleted for version-on-write
    /// semantics. The POCO must expose these columns so the SaveKeyFields retire step can emit
    /// UPDATE SET IsCurrent=false without a raw SQL workaround.
    /// </remarks>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }
}
