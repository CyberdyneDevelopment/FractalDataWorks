using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions.Health.Monitoring.Logging;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.HealthChecks.Monitoring;
using Fdw.Services.Logging;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// Builds the HttpHealthMonitor implementation.
/// </summary>
/// <remarks>
/// Nothing to resolve here, so it completes synchronously. The provider contract is asynchronous
/// because RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class HttpHealthMonitorProvider
    : ImplementationServiceProviderBase<IHealthMonitorService, IHealthMonitorImplementationConfiguration>,
      IHttpHealthMonitorProvider
{
    private readonly IHttpHealthMonitorFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHealthMonitorProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the service.</param>
    public HttpHealthMonitorProvider(IHttpHealthMonitorFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IHealthMonitorService>> Create(
        IHealthMonitorImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_factory.Create(configuration));
}
