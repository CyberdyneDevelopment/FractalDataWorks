using Fdw.Configuration;
using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Base class for data store type definitions.
/// Provides configuration binding, builder supply, and provider integration.
/// </summary>
/// <typeparam name="TConfiguration">The configuration type for the data store.</typeparam>
public abstract class DataStoreTypeBase<TConfiguration> :
    GuidTypeOptionBase<IDataStoreType>,
    IDataStoreType<TConfiguration>
    where TConfiguration : class, IGenericConfiguration
{
    /// <summary>
    /// Gets the configuration type.
    /// </summary>
    public Type ConfigurationType => typeof(TConfiguration);

    /// <summary>
    /// Initializes a new instance of the DataStoreTypeBase class.
    /// </summary>
    protected DataStoreTypeBase(
        string name,
        string displayName,
        string description,
        string? category = null)
        : base(name, displayName, description, category ?? "DataStore")
    {
    }

    /// <summary>
    /// Supplies the per-transport <see cref="IDataStoreBuilder"/> for this transport.
    /// </summary>
    /// <param name="logger">Logger for build diagnostics.</param>
    /// <returns>A fresh builder instance for one store build.</returns>
    /// <remarks>
    /// Why: each transport option constructs its own builder in its own assembly (which references
    /// <c>Services.Data</c> where the concrete builders live); the abstraction layer cannot construct
    /// them without a reference cycle. Replaces the never-called per-container <c>Build(...)</c>.
    /// </remarks>
    public abstract IDataStoreBuilder SupplyBuilder(ILogger? logger = null);

    /// <inheritdoc />
    /// <remarks>False unless the transport overrides it: a SQL or HTTP store finds a container by its own name.</remarks>
    public virtual bool AddressesContainersAsFiles => false;

}
