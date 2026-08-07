using System;
using Fdw.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.EtlMappers.Abstractions;

/// <summary>
/// Interface for ETL row mapper type definitions.
/// Mapper types define how to configure, create, and register mappers.
/// </summary>
public interface IEtlRowMapperType : ITypeOption<Guid, IEtlRowMapperType>
{
    /// <summary>
    /// Gets the configuration section name for appsettings.json.
    /// </summary>
    string SectionName { get; }

    /// <summary>
    /// Gets the display name for this mapper type.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the description of what this mapper type provides.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the configuration type for this mapper type.
    /// </summary>
    Type ConfigurationType { get; }

    /// <summary>
    /// Gets the factory type for creating mapper instances.
    /// </summary>
    Type FactoryType { get; }

    /// <summary>
    /// Gets the estimated allocations per row for this mapper type.
    /// </summary>
    int EstimatedAllocationsPerRow { get; }

    /// <summary>
    /// Configures IOptions binding for this mapper type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    void Configure(IServiceCollection services, IConfiguration configuration, ILoggerFactory? loggerFactory = null);

    /// <summary>
    /// Registers required services (factories) with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <returns>The service collection for chaining.</returns>
    IServiceCollection Register(IServiceCollection services, ILoggerFactory? loggerFactory = null);

}

/// <summary>
/// Generic interface for mapper types with typed configuration and factory.
/// </summary>
/// <typeparam name="TMapper">The mapper type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type.</typeparam>
/// <typeparam name="TFactory">The factory type.</typeparam>
public interface IEtlRowMapperType<TMapper, TFactory, TConfiguration> : IEtlRowMapperType
    where TMapper : IEtlRowMapper
    where TConfiguration : EtlRowMapperConfiguration
    where TFactory : IEtlRowMapperFactory<TMapper, TConfiguration>
{
}
