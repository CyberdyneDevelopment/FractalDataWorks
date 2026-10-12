using System;
using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>Marker for the hosting option set — the return type of ById, ByName and All.</summary>
public interface IHostType : IServiceType
{
}

/// <summary>The closed form every hosting option satisfies.</summary>
/// <typeparam name="TService">The hosting service the option's factory creates.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration it binds to.</typeparam>
/// <typeparam name="TFactory">The factory the option registers.</typeparam>
public interface IHostType<TService, TConfiguration, TFactory>
    : IServiceType<Guid, TService, TFactory, TConfiguration>, IHostType
    where TService : IHostService
    where TConfiguration : IGenericConfiguration
    where TFactory : IHostFactory<TService, TConfiguration>
{
}
