using Fdw.ServiceTypes;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// A service type that registers the <see cref="IGenericScheduledExecutionHandler"/> a scheduler hands
/// a firing schedule to.
/// </summary>
/// <remarks>
/// A scheduler decides WHEN; the handler decides HOW the scheduled process runs (for example, by
/// triggering a pipeline on the ETL server). Which handler a host uses is chosen by referencing its
/// registration package, the same way a scheduler engine is chosen.
/// </remarks>
public interface IScheduledExecutionHandlerType : IServiceType
{
}
