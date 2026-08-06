using System;
using System.Reflection;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Internal entity for DataStoreConfiguration table.
/// </summary>
public class DataStoreRecord
{
    /// <summary>Gets or sets the configuration identifier.</summary>
    public Guid ConfigurationId { get; set; }
    /// <summary>Gets or sets the data store type name.</summary>
    public string? DataStoreTypeName { get; set; }
    /// <summary>Gets or sets the store type.</summary>
    public string StoreType { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical location.</summary>
    public string Location { get; set; } = string.Empty;
    /// <summary>Gets or sets the translator type used for data access.</summary>
    public string TranslatorType { get; set; } = string.Empty;
}