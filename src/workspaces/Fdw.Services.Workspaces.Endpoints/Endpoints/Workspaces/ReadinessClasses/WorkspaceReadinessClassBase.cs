using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The base of every place a data set can stand on the binding scale.</summary>
[ExcludeFromCodeCoverage]
public abstract class WorkspaceReadinessClassBase : TypeOptionBase<WorkspaceReadinessClassBase>, IWorkspaceReadinessClass
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceReadinessClassBase"/> class.</summary>
    /// <param name="name">The class's name, which the report's counts are named for.</param>
    protected WorkspaceReadinessClassBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract IGenericResult Tally(WorkspaceReadinessResponse response, ICollection<string> blocked, string dataSetName, ILogger logger);
}
