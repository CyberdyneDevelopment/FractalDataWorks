using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The places a data set can stand on the binding scale: Bound, Partial, Sketched and Proposed.</summary>
[TypeCollection(typeof(WorkspaceReadinessClassBase), typeof(IWorkspaceReadinessClass), typeof(WorkspaceReadinessClasses))]
[ExcludeFromCodeCoverage]
public abstract partial class WorkspaceReadinessClasses : TypeCollectionBase<WorkspaceReadinessClassBase, IWorkspaceReadinessClass>
{
    // The generated NotFound sentinel counts nothing: the report has no class of that name, which is the platform's own fault.
    private sealed partial class NotFoundWorkspaceReadinessClasses
    {
        /// <inheritdoc />
        public override IGenericResult Tally(WorkspaceReadinessResponse response, System.Collections.Generic.ICollection<string> blocked, string dataSetName, ILogger logger)
            => GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceReadinessClassUnknown"), logger, ResultDetails.Create("dataSet", dataSetName));
    }
}
