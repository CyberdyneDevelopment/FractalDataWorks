using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The data set is named and nothing more: no fields, or fields with no types. A data set that cannot be read counts here too. It blocks materialization.</summary>
[TypeOption(typeof(WorkspaceReadinessClasses), "Proposed")]
[ExcludeFromCodeCoverage]
public sealed class ProposedWorkspaceReadinessClass : WorkspaceReadinessClassBase
{
    /// <summary>Initializes a new instance of the <see cref="ProposedWorkspaceReadinessClass"/> class.</summary>
    public ProposedWorkspaceReadinessClass()
        : base("Proposed")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Tally(WorkspaceReadinessResponse response, ICollection<string> blocked, string dataSetName, ILogger logger)
    {
        response.Proposed++;
        blocked.Add(dataSetName);
        return GenericResult.Success();
    }
}
