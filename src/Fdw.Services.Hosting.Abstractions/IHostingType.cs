using System;
using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.Hosting.Abstractions;

/// <summary>Marker for the hosting option set — the return type of ById, ByName and All.</summary>
public interface IHostingType : IServiceType
{
}

/// <summary>The closed form every hosting option satisfies.</summary>
/// <typeparam name="TService">The hosting service the option's factory creates.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration it binds to.</typeparam>
/// <typeparam name="TFactory">The factory the option registers.</typeparam>
public interface IHostingType<TService, TConfiguration, TFactory>
    : IServiceType<Guid, TService, TFactory, TConfiguration>, IHostingType
    where TService : IHostingService
    where TConfiguration : IGenericConfiguration
    where TFactory : IHostingFactory<TService, TConfiguration>
{
}
