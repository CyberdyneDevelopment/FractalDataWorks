using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Lists the thresholds, one page at a time (FDW-3948): each with its type, the kind of subject it judges, whether it is judged and how many rules it has.</summary>
public abstract class ListThresholdsEndpointBase : CrudPageEndpointBase<IThresholdImplementationConfiguration, ThresholdSummaryResponse>
{
    private readonly IThresholdConfigurationProvider _thresholds;

    /// <summary>Initializes a new instance of the <see cref="ListThresholdsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="thresholds">The thresholds.</param>
    protected ListThresholdsEndpointBase(ILogger<ListThresholdsEndpointBase> logger, IThresholdConfigurationProvider thresholds)
        : base(logger)
    {
        _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
    }

    /// <inheritdoc />
    protected override string ResourceName => "thresholds";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<IThresholdImplementationConfiguration>>> LoadPage(PaginatedListRequest paging, CancellationToken ct) =>
        _thresholds.Get(paging.Skip, paging.Take, ConfigurationLevels.WholeAggregate, ct);

    /// <inheritdoc />
    protected override Task<IGenericResult<ThresholdSummaryResponse>> ToSummary(IThresholdImplementationConfiguration item, CancellationToken ct) =>
        Task.FromResult(GenericResult<ThresholdSummaryResponse>.Success(ThresholdMapper.ToSummary(item)));
}
