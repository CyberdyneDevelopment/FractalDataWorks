using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One place a data set can stand on the binding scale, and what a data set standing there adds to the readiness report.</summary>
public interface IWorkspaceReadinessClass : ITypeOption<int, WorkspaceReadinessClassBase>
{
    /// <summary>Counts one data set of this class into the report.</summary>
    /// <param name="response">The report.</param>
    /// <param name="blocked">The names of the data sets preventing materialization.</param>
    /// <param name="dataSetName">The data set's name.</param>
    /// <param name="logger">Records the refusal.</param>
    /// <returns>Success, or the refusal when the class is the one a lookup found nothing for.</returns>
    IGenericResult Tally(WorkspaceReadinessResponse response, ICollection<string> blocked, string dataSetName, ILogger logger);
}
