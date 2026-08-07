using System;
using Fdw.Collections;
using Fdw.Services.EtlMappers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.EtlMappers;

/// <summary>
/// Base class for ETL row mapper service types.
/// Provides common functionality for mapper type definitions.
/// </summary>
/// <typeparam name="TMapper">The mapper service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating mapper instances.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the mapper.</typeparam>
public abstract class EtlRowMapperTypeBase<TMapper, TFactory, TConfiguration>
    : TypeOptionBase<Guid, IEtlRowMapperType>,
      IEtlRowMapperType<TMapper, TFactory, TConfiguration>
    where TMapper : IEtlRowMapper
    where TConfiguration : EtlRowMapperConfiguration
    where TFactory : IEtlRowMapperFactory<TMapper, TConfiguration>
{
    /// <summary>
    /// Gets the configuration section name for appsettings.json.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Gets the display name for this mapper type.
    /// </summary>
    public new string DisplayName { get; }

    /// <summary>
    /// Gets the description of what this mapper type provides.
    /// </summary>
    public new string Description { get; }

    /// <inheritdoc />
    public Type ConfigurationType => typeof(TConfiguration);

    /// <inheritdoc />
    public Type FactoryType => typeof(TFactory);

    /// <summary>
    /// Gets the estimated allocations per row for this mapper type.
    /// </summary>
    public abstract int EstimatedAllocationsPerRow { get; }

    /// <summary>
    /// Initializes a new instance of the mapper type base class.
    /// </summary>
    /// <param name="name">The name of this mapper type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name for this mapper type.</param>
    /// <param name="description">The description of what this mapper type provides.</param>
    protected EtlRowMapperTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description)
        : base(Guid.NewGuid(), name)
    {
        SectionName = sectionName;
        DisplayName = displayName;
        Description = description;
    }

    /// <inheritdoc />
    public virtual void Configure(IServiceCollection services, IConfiguration configuration, ILoggerFactory? loggerFactory = null)
    {
        // Default: bind configuration section to IOptions<List<TConfiguration>>
        var section = configuration.GetSection(SectionName);
        if (section.Exists())
        {
            services.Configure<System.Collections.Generic.List<TConfiguration>>(section);
        }
    }

    /// <inheritdoc />
    public abstract IServiceCollection Register(IServiceCollection services, ILoggerFactory? loggerFactory = null);

}
