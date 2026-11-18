using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// One working area of a data store (FDW-6394): the name of a path or schema whose tables exist only while a pipeline runs, such as a staging schema.
/// </summary>
/// <remarks>
/// A row of the store, like <see cref="DataPathConfiguration"/>, because the mapper stores a collection only when its elements are configuration records. The store reads
/// the names back as <see cref="IDataStoreImplementationConfiguration.WorkingPaths"/>; a store with no rows has no working area, and nothing is assumed.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataStore", ServiceType = "DataStoreWorkingPath")]
public partial class DataStoreWorkingPathConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier of this row, kept across updates while the store states the same name.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the path or schema name, compared without regard to case.</summary>
    public string Name { get; set; } = string.Empty;
}
