using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Scheduling;

/// <summary>Configuration provider for schedule configurations. Thin wrapper over
/// <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/>.</summary>
/// <remarks>
/// Raises <see cref="Changed"/> after a successful write so a scheduler implementation's own
/// reconciliation loop can react immediately rather than waiting for its next poll — configuration
/// is the one source of truth for which schedules should be live, so a write here is the moment
/// that changes.
/// </remarks>
public class ScheduleConfigurationProvider : ImplementationConfigurationProviderBase<ScheduleConfiguration, ScheduleConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleConfigurationProvider"/> class.</summary>
    public ScheduleConfigurationProvider(
        ILogger<ScheduleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sched")
        : base(logger ?? NullLogger<ScheduleConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    /// <summary>
    /// Raised after a successful <see cref="Save"/> or <see cref="Delete(Guid, CancellationToken)"/>.
    /// A scheduler implementation's reconciliation loop subscribes to trigger an immediate pass.
    /// </summary>
    public event EventHandler? Changed;

    /// <inheritdoc />
    public override async Task<IGenericResult<ScheduleConfiguration>> Save(ScheduleConfiguration record, CancellationToken ct = default)
    {
        var result = await base.Save(record, ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await base.Delete(id, ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult> Delete(string name, CancellationToken ct = default)
    {
        var result = await base.Delete(name, ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }
}
