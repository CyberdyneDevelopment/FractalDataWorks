using Fdw.Results;
using Fdw.Services.Data.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Refuses a data query as <c>DataQueryInvalid</c>, naming the data set.</summary>
internal sealed class DataQueryRefusals : IQueryRefusals
{
    private readonly string _dataSetName;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DataQueryRefusals"/> class.</summary>
    /// <param name="dataSetName">The data set queried, for refusals.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    public DataQueryRefusals(string dataSetName, ILogger logger)
    {
        _dataSetName = dataSetName;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IGenericResult<T> Invalid<T>(string reason)
        => GenericResult<T>.Failure(
            DataServiceResultCodes.ByName("DataQueryInvalid"), _logger,
            ResultDetails.Create("dataSet", _dataSetName, "reason", reason));
}
