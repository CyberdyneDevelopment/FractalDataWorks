using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Web.Clients.Abstractions.Registration;

/// <summary>
/// Generic marker factory interface for API client types.
/// Each concrete <typeparamref name="TClient"/> produces a unique closed generic type, which ensures
/// <c>ServiceTypeBase&lt;TService, TFactory&gt;.Id</c> computes a unique GUID per client type.
/// </summary>
/// <typeparam name="TClient">The concrete API client class or interface
/// (e.g., <c>ConnectionApiClient</c>, <c>IPipelineClient</c>).</typeparam>
/// <remarks>
/// This interface has no members — it exists purely as a type-level discriminator for
/// <see cref="ApiClientTypeBase{TClient}"/>. It extends <see cref="IServiceFactory{TService}"/>
/// to satisfy the <c>ServiceTypeBase&lt;TService, TFactory&gt;</c> constraint.
/// </remarks>
public interface IApiClientFactory<TClient> : IServiceFactory<IGenericService, IServiceConfiguration>
    where TClient : class
{
}
