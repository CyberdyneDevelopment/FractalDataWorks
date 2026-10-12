using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Base for a service type that registers an <see cref="IGenericScheduledExecutionHandler"/>.
/// </summary>
/// <typeparam name="THandler">The handler this option registers.</typeparam>
/// <remarks>
/// A derived option registers its handler in its Registration phase. A host references exactly one
/// handler package: which process runner a scheduler hands its fires to is the developer's choice.
/// </remarks>
public abstract class ScheduledExecutionHandlerTypeBase<THandler>
    : ServiceTypeBase<IGenericService, IScheduledExecutionHandlerFactory<THandler>, IServiceConfiguration>,
      IScheduledExecutionHandlerType
    where THandler : class, IGenericScheduledExecutionHandler
{
    /// <summary>Initializes a new instance of the <see cref="ScheduledExecutionHandlerTypeBase{THandler}"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="displayName">The display name.</param>
    protected ScheduledExecutionHandlerTypeBase(string name, string displayName)
        : base(name, "ScheduledExecutionHandler", displayName, $"{displayName} scheduled execution handler")
    {
    }
}
