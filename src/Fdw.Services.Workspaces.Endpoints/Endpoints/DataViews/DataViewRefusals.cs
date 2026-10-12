using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Refuses a view with the result code it is given, naming the view and the first thing that did not resolve.</summary>
internal sealed class DataViewRefusals : IQueryRefusals
{
    private readonly string _viewName;
    private readonly ILogger _logger;
    private readonly string _resultCode;

    /// <summary>Initializes a new instance of the <see cref="DataViewRefusals"/> class.</summary>
    /// <param name="viewName">The view being saved, for refusals.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <param name="resultCode">The name of the result code a refusal carries: <c>DataViewSpecInvalid</c> or <c>DataSetVariationInvalid</c>.</param>
    public DataViewRefusals(string viewName, ILogger logger, string resultCode)
    {
        _viewName = viewName;
        _logger = logger;
        _resultCode = resultCode;
    }

    /// <inheritdoc/>
    public IGenericResult<T> Invalid<T>(string reason)
        => GenericResult<T>.Failure(
            WorkspacesResultCodes.ByName(_resultCode), _logger,
            ResultDetails.Create("name", _viewName, "reason", reason));
}
