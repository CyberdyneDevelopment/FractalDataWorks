using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Fdw.ServiceTypes;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Commands;
using Fdw.Services.Etl;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// The StreamingPipeline implementation of its domain.
/// </summary>
/// <remarks>
/// A per-option interface for the same reason <c>IStreamingPipelineFactory</c> has one: each implementation
/// closes its base with its OWN interface, and that is what the domain provider registers against.
/// </remarks>
public interface IStreamingPipelineProvider
    : IImplementationServiceProvider<IEtlPipeline, IPipelineImplementationConfiguration>
{
}
