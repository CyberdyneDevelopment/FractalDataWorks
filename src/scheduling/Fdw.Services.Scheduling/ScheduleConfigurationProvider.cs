using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling;

/// <summary>Supplies the Schedule configuration.</summary>
/// <remarks>
/// A schedule's <c>Description</c> is not a column of <c>sched.ScheduleConfiguration</c> (FDW-6208): it is a row of the descriptions table under the description type
/// <c>Schedule</c>, written in the unit of the schedule's save, filled once for every set of composed schedules this provider reads, and removed with the schedule.
/// </remarks>
public sealed class ScheduleConfigurationProvider
    : DomainConfigurationProviderBase<IScheduleImplementationConfiguration>,
      IScheduleConfigurationProvider,
      IConfigurationWriteObserver<IScheduleImplementationConfiguration>,
      IConfigurationReadObserver<IScheduleImplementationConfiguration>,
      IReferencedConfigurationProvider<IScheduleImplementationConfiguration>
{
    private const string DescriptionType = ScheduleDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="ScheduleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each schedule's description in the unit of the schedule's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the schedules a read has just composed, once for the set (FDW-6208).</param>
    public ScheduleConfigurationProvider(
        ILogger<ScheduleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IDescriptionWriter writer,
        IDescriptionReader reader)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _writer = writer;
        _reader = reader;
    }

    /// <inheritdoc />
    /// <remarks>A schedule saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(IScheduleImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A schedule with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<IScheduleImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, schedule => schedule.Id, (schedule, text) => schedule.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A schedule's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
