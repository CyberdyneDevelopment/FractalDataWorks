using Fdw.Configuration;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// One configured scheduler — the domain record, naming which implementation it is and holding that
/// implementation's own configuration.
/// </summary>
public interface ISchedulerConfiguration
    : IPlatformServiceConfiguration<ISchedulerImplementationConfiguration>
{
}
