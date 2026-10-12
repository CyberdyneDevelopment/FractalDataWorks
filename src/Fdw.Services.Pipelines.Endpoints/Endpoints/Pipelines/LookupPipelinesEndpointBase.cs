using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Base endpoint for looking up pipelines by name (FDW-1194), for pickers and name-taken checks.
/// </summary>
/// <remarks>
/// One query on the pipelines domain table: no pipeline's implementation or stages are read. It offers the rows the pipeline
/// list offers: a pipeline with no name is never offered.
/// </remarks>
public abstract class LookupPipelinesEndpointBase : LookupEndpointBase<DomainConfiguration>
{
    private readonly PipelineServiceConfigurationProvider _configProvider;

    /// <summary>Initializes a new instance of the <see cref="LookupPipelinesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configProvider">Reads the page of pipeline rows.</param>
    protected LookupPipelinesEndpointBase(ILogger<LookupPipelinesEndpointBase> logger, PipelineServiceConfigurationProvider configProvider)
        : base(logger)
    {
        _configProvider = configProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "pipelines";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> Read(DomainQuery query, CancellationToken ct)
        => _configProvider.Get(query, ct);

    /// <summary>Leaves out the rows the pipeline list leaves out.</summary>
    /// <param name="row">The pipeline row.</param>
    /// <returns>True to offer the pipeline.</returns>
    protected override bool Includes(DomainConfiguration row) => !string.IsNullOrWhiteSpace(row.Name);
}
