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
    TypeOptionBase<Guid, IDataStoreType>,
    IDataStoreType<TConfiguration>
    where TConfiguration : class, IGenericConfiguration
{
    /// <summary>
    /// Generates a deterministic Guid from a name string.
    /// </summary>
#pragma warning disable CA5351, SCS0006, CA1850 // MD5 used for deterministic ID generation, not cryptographic security
    private static Guid GenerateId(string name)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"DataStoreType:{name}"));
        return new Guid(hash);
    }
#pragma warning restore CA5351, SCS0006, CA1850

    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Gets the configuration type.
    /// </summary>
    public Type ConfigurationType => typeof(TConfiguration);

    /// <summary>
    /// Initializes a new instance of the DataStoreTypeBase class.
    /// </summary>
    protected DataStoreTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(GenerateId(name), name, sectionName, displayName, description, category ?? "DataStore")
    {
        SectionName = sectionName;
    }

    /// <summary>
    /// Configures IOptions binding for this data store type.
    /// Binds List&lt;TConfiguration&gt; from the configuration section.
    /// </summary>
    public virtual void Configure(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        services.Configure<List<TConfiguration>>(section);
    }

    /// <summary>
    /// Configures IOptions binding with logging support.
    /// </summary>
    public virtual void Configure(IServiceCollection services, IConfiguration configuration, ILoggerFactory? loggerFactory)
    {
        Configure(services, configuration);
    }

    /// <summary>
    /// Registers required services (factory + dependencies) with the DI container.
    /// </summary>
    public abstract IServiceCollection Register(IServiceCollection services);

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

}
