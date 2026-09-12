using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Configuration;

/// <summary>
/// Resolves configured schedules and routes each to the implementation provider that owns it.
/// </summary>
/// <remarks>
/// Named rather than consumed as a bare <c>IDomainConfigurationProvider&lt;IScheduleImplementationConfiguration&gt;</c>,
/// for the reason <see cref="Fdw.Services.Scheduling.Abstractions.ISchedulerConfigurationProvider"/> is:
/// a consumer asking for the closed generic states a shape, this states which rows it reads. Schedule
/// was the one domain without its named interface, which is why its list endpoint reached for an
/// implementation provider instead and found nothing registered under it.
/// <para>
/// It sits beside <see cref="IScheduleImplementationConfiguration"/> in this project rather than in
/// Scheduling.Abstractions, because that contract lives here despite its namespace and Abstractions
/// cannot reference back into this assembly.
/// </para>
/// </remarks>
public interface IScheduleConfigurationProvider
    : IDomainConfigurationProvider<IScheduleImplementationConfiguration>
{
}
