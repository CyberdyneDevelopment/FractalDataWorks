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
        : base(name, "ApiClients", displayName, $"{displayName} HTTP client")
    {
    }

    /// <inheritdoc />
    // Why an invoker override instead of Registration(...) in the constructor: every one of the ~35
    // concrete client types sets its own Registration body, and the gerund setter REPLACES rather than
    // composes — so a base-constructor call is overwritten by the derived constructor that runs after
    // it, and BearerTokenHandler was never registered for any client. Each client attaches the handler
    // to its named HttpClient via AddBearerTokenHandler(), which only adds it to the pipeline; without
    // this the host throws at first client construction:
    //   No service for type 'Fdw.Web.Http.Authentication.BearerTokenHandler' has been registered.
    public override IGenericResult<IHostApplicationBuilder> Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory,
        string dataStoreName,
        string pathName,
        string containerName)
    {
        if (builder is null) throw new System.ArgumentNullException(nameof(builder));

        builder.Services.AddTransient<BearerTokenHandler>();

        return base.Register(builder, loggerFactory, dataStoreName, pathName, containerName);
    }

    /// <summary>
    /// Resolves the base URL for THIS client: the per-client entry
    /// <c>ApiClients:{Name}:BaseUrl</c> when the host declares one, otherwise the host-wide
    /// <c>ApiClients:BaseUrl</c>. Returns null when the host declares neither.
    /// </summary>
    /// <param name="configuration">The host's configuration.</param>
    /// <returns>The resolved base URL, or null when no URL is declared for this client.</returns>
    /// <remarks>
    /// Why this lives on the base rather than at each call site: every client type registers its OWN
    /// named <see cref="System.Net.Http.HttpClient"/> keyed by <c>Name</c>, so per-client endpoints were
    /// always physically possible — but each of the ~35 call sites independently read the flat
    /// <c>ApiClients:BaseUrl</c>, which collapsed them all onto one URL and left the per-client shape
    /// unread. Reference.Api declares <c>ApiClients:PipelineJobClient:BaseUrl</c> and
    /// <c>ApiClients:ScheduleClient:BaseUrl</c> to reach the ETL and Scheduler hosts; because nothing
    /// resolved that shape — and Reference.Api declares no flat key — those clients were registered with
    /// no BaseAddress at all and failed at call time. One resolution point fixes every client at once and
    /// is what lets a host point individual clients at different endpoints.
    /// <para>
    /// This is a declared-override hierarchy (most specific declared value wins), NOT a fallback default:
    /// both keys are values the operator wrote, and neither is invented here. When the host declares
    /// neither, this returns null and the caller registers nothing rather than guessing a URL.
    /// </para>
    /// </remarks>
    protected string? ResolveBaseUrl(IConfiguration configuration)
    {
        if (configuration is null) throw new System.ArgumentNullException(nameof(configuration));

        return configuration[$"ApiClients:{Name}:BaseUrl"] ?? configuration["ApiClients:BaseUrl"];
    }

}
