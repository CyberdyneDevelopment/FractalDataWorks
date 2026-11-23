using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Some bindable fields are bound and some are not; the data set blocks materialization.</summary>
[TypeOption(typeof(WorkspaceReadinessClasses), "Partial")]
[ExcludeFromCodeCoverage]
public sealed class PartialWorkspaceReadinessClass : WorkspaceReadinessClassBase
{
    /// <summary>Initializes a new instance of the <see cref="PartialWorkspaceReadinessClass"/> class.</summary>
    public PartialWorkspaceReadinessClass()
        : base("Partial")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Tally(WorkspaceReadinessResponse response, ICollection<string> blocked, string dataSetName, ILogger logger)
    {
        response.Partial++;
        blocked.Add(dataSetName);
        return GenericResult.Success();
    }
}
