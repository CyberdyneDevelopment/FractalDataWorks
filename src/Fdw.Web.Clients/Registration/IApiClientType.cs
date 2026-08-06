using Fdw.ServiceTypes;

namespace Fdw.Web.Clients.Abstractions.Registration;

/// <summary>
/// Non-generic marker interface for API client type options.
/// </summary>
/// <remarks>
/// A ServiceTypeCollection requires both a generic interface (with full type parameters extending
/// <c>IServiceType&lt;Guid, TService, TFactory, TConfiguration&gt;</c>) and a non-generic marker
/// interface extending <see cref="IServiceType"/>. See <c>IConnectionType</c> /
/// <c>IConnectionType&lt;TService, TConfiguration, TFactory&gt;</c> for the reference pattern.
/// </remarks>
public interface IApiClientType : IServiceType { }
