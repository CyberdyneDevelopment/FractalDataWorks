using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Every bindable field is bound; the data set blocks nothing.</summary>
[TypeOption(typeof(WorkspaceReadinessClasses), "Bound")]
[ExcludeFromCodeCoverage]
public sealed class BoundWorkspaceReadinessClass : WorkspaceReadinessClassBase
{
    /// <summary>Initializes a new instance of the <see cref="BoundWorkspaceReadinessClass"/> class.</summary>
    public BoundWorkspaceReadinessClass()
        : base("Bound")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Tally(WorkspaceReadinessResponse response, ICollection<string> blocked, string dataSetName, ILogger logger)
    {
        response.Bound++;
        return GenericResult.Success();
    }
}
