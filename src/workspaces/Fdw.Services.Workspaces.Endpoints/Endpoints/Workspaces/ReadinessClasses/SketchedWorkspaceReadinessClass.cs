using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The fields have types and nothing is bound; the data set blocks materialization.</summary>
[TypeOption(typeof(WorkspaceReadinessClasses), "Sketched")]
[ExcludeFromCodeCoverage]
public sealed class SketchedWorkspaceReadinessClass : WorkspaceReadinessClassBase
{
    /// <summary>Initializes a new instance of the <see cref="SketchedWorkspaceReadinessClass"/> class.</summary>
    public SketchedWorkspaceReadinessClass()
        : base("Sketched")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Tally(WorkspaceReadinessResponse response, ICollection<string> blocked, string dataSetName, ILogger logger)
    {
        response.Sketched++;
        blocked.Add(dataSetName);
        return GenericResult.Success();
    }
}
