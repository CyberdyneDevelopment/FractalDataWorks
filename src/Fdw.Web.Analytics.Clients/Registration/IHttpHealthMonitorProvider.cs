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
using Fdw.Services.Abstractions;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// The HttpHealthMonitor implementation of its domain.
/// </summary>
/// <remarks>
/// A per-option interface for the same reason <c>IHttpHealthMonitorFactory</c> has one: each implementation
/// closes its base with its OWN interface, and that is what the domain provider registers against.
/// </remarks>
public interface IHttpHealthMonitorProvider
    : IImplementationServiceProvider<IHealthMonitorService, IHealthMonitorImplementationConfiguration>
{
}
