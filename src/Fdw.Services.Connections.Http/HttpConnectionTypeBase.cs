using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Connections.Http.Abstractions;

/// <summary>
/// Abstract base class for HTTP connection service types.
/// Concrete implementations should inherit from this class and specify their specific service implementation.
/// </summary>
/// <typeparam name="TService">The connection service interface type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the connection service.</typeparam>
/// <typeparam name="TFactory">The factory type for creating connection service instances.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class HttpConnectionTypeBase<TService, TConfiguration, TFactory> :
    ConnectionTypeBase<TService, TFactory, TConfiguration>
    where TService : IGenericConnection
    // Why: typed body configs are standalone POCOs implementing IGenericConfiguration directly;
    // they no longer inherit from ConnectionConfiguration after the config-split refactor.
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IConnectionFactory<TService, TConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnectionTypeBase{TService,TConfiguration,TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of the HTTP connection type.</param>
    /// <param name="sectionName">The configuration section name for appsettings.json.</param>
    /// <param name="displayName">The display name for this service type.</param>
    /// <param name="description">The description of what this service type provides.</param>
    /// <param name="category">The category for this HTTP connection type (defaults to "HTTP Connection").</param>
    protected HttpConnectionTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "HTTP Connection")
    {
    }
}