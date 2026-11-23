using System;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The host's <see cref="IPipelineRunOrigins"/> (FDW-6519): the application name the platform states and the slot name the one reader of the slot file states.
/// </summary>
public sealed class PipelineRunOrigins : IPipelineRunOrigins
{
    private readonly IHostEnvironment _environment;
    private readonly ISlotRole _slot;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunOrigins"/> class.</summary>
    /// <param name="environment">The host environment, whose application name is the executing host.</param>
    /// <param name="slot">The slot role, whose slot name is the executing slot.</param>
    /// <param name="logger">The logger.</param>
    public PipelineRunOrigins(IHostEnvironment environment, ISlotRole slot, ILogger<PipelineRunOrigins>? logger = null)
    {
        _environment = environment;
        _slot = slot;
        _logger = logger ?? NullLogger<PipelineRunOrigins>.Instance;
    }

    /// <inheritdoc />
    public IGenericResult<PipelineRunOrigin> Of(Guid? actingPersonId, string? runMode)
    {
        if (string.IsNullOrWhiteSpace(runMode))
            return GenericResult<PipelineRunOrigin>.Failure(PipelineRunStartLog.RunOriginIncomplete(_logger, "the run mode"));

        if (string.IsNullOrWhiteSpace(_environment.ApplicationName))
            return GenericResult<PipelineRunOrigin>.Failure(PipelineRunStartLog.RunOriginIncomplete(_logger, "the host's application name"));

        return GenericResult<PipelineRunOrigin>.Success(new PipelineRunOrigin(actingPersonId, runMode, _environment.ApplicationName, _slot.SlotName));
    }
}
