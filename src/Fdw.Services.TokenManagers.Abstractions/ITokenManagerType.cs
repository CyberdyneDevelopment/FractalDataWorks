using System;
using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Interface for token manager service types. Mirrors <c>ISchedulerType</c>'s generic/non-generic
/// split, but stays a pure marker (no domain-specific capability properties) — the TokenManagers
/// domain is a "declared choice" (<c>Manual = true</c>) with exactly one active provider per host,
/// not a menu of interchangeable engines with differing capabilities.
/// </summary>
/// <typeparam name="TService">The token manager service interface type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the token manager service.</typeparam>
/// <typeparam name="TFactory">The factory type for creating token manager service instances.</typeparam>
public interface ITokenManagerType<TService, TConfiguration, TFactory> : IServiceType<Guid, TService, TFactory, TConfiguration>, ITokenManagerType
    where TService : ITokenManager
    where TConfiguration : IGenericConfiguration
    where TFactory : ITokenManagerFactory<TService, TConfiguration>
{
}

/// <summary>
/// Non-generic interface for token manager service types.
/// </summary>
public interface ITokenManagerType : IServiceType
{
}
