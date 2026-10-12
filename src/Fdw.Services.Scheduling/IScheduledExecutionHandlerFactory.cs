using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling.Abstractions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Identifies one scheduled execution handler option.
/// </summary>
/// <typeparam name="THandler">The handler the option registers.</typeparam>
/// <remarks>
/// Why generic over the handler: a service type's Id is derived from its service and factory type
/// names, so each handler option needs a factory type of its own (the IApiClientFactory&lt;TClient&gt; shape).
/// </remarks>
public interface IScheduledExecutionHandlerFactory<THandler> : IServiceFactory<IGenericService, IServiceConfiguration>
    where THandler : class, IGenericScheduledExecutionHandler
{
}
