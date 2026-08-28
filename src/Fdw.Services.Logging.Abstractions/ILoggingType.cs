using System;
using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.Logging.Abstractions;

/// <summary>Marker for the logging option set — the return type of ById, ByName and All.</summary>
public interface ILoggingType : IServiceType
{
}

/// <summary>The closed form every logging option satisfies.</summary>
/// <typeparam name="TService">The logging service the option's factory creates.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration it binds to.</typeparam>
/// <typeparam name="TFactory">The factory the option registers.</typeparam>
public interface ILoggingType<TService, TConfiguration, TFactory>
    : IServiceType<Guid, TService, TFactory, TConfiguration>, ILoggingType
    where TService : ILoggingService
    where TConfiguration : IGenericConfiguration
    where TFactory : ILoggingFactory<TService, TConfiguration>
{
}
