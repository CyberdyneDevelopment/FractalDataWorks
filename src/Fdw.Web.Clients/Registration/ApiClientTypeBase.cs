using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Results;

namespace Fdw.Web.Clients.Abstractions.Registration;

/// <summary>
/// Base class for API client type options.
/// </summary>
/// <typeparam name="TClient">The concrete API client class or interface that uniquely identifies
/// this client type (e.g., <c>IPipelineClient</c>, <c>ConnectionApiClient</c>).
/// Each concrete option must provide a unique <typeparamref name="TClient"/> to ensure a unique
/// <c>ServiceTypeBase.Id</c>.</typeparam>
/// <remarks>
/// <para>
/// The <typeparamref name="TClient"/> parameter flows into <see cref="IApiClientFactory{TClient}"/>
/// which is used as the TFactory in <c>ServiceTypeBase&lt;IGenericService, IApiClientFactory&lt;TClient&gt;&gt;</c>.
/// Since <c>ServiceTypeBase.Id</c> is computed from <c>typeof(TFactory).FullName</c>, each unique
/// <typeparamref name="TClient"/> produces a unique Id.
/// </para>
/// </remarks>
public abstract class ApiClientTypeBase<TClient>
    : ServiceTypeBase<IGenericService, IApiClientFactory<TClient>, IServiceConfiguration>, IApiClientType
    where TClient : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientTypeBase{TClient}"/> class.
    /// </summary>
    protected ApiClientTypeBase(string name, string displayName)
        : base(name, "ApiClient", displayName, $"{displayName} HTTP client")
    {

    }


}
