using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Confirms a relationship's join field actually belongs to the data set it's named against.</summary>
/// <remarks>
/// Shared by <see cref="CreateWorkspaceRelationshipEndpointBase"/> and
/// <see cref="UpdateWorkspaceRelationshipEndpointBase"/> -- both write LeftFieldId/RightFieldId onto
/// the same row shape, and a field id from the wrong data set is the same defect from either path.
/// </remarks>
internal static class WorkspaceFieldOwnershipValidator
{
    /// <summary>Validates one side of a relationship's join field, when one was supplied.</summary>
    /// <param name="dataSets">Reads the data set the field is claimed to belong to.</param>
    /// <param name="logger">Logs the failure when the field doesn't resolve.</param>
    /// <param name="workspaceName">The workspace the relationship belongs to, for the error.</param>
    /// <param name="side">"left" or "right" -- which join field this is.</param>
    /// <param name="dataSetId">The data set this side of the relationship names.</param>
    /// <param name="fieldId">The field id to validate, or null when this side isn't being set.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    public static async Task<IGenericResult> Validate(
        IDataSetConfigurationProvider dataSets,
        ILogger logger,
        string workspaceName,
        string side,
        Guid dataSetId,
        Guid? fieldId,
        CancellationToken ct)
    {
        if (fieldId is null) return GenericResult.Success();

        var dataSet = await dataSets.Get(dataSetId, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet;

        if (dataSet.Value is null || dataSet.Value.Fields.All(f => f.Id != fieldId.Value))
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceChildNotFound"), logger,
                ResultDetails.Create("name", workspaceName, "kind", $"{side} field", "id", fieldId.Value.ToString()));
        }

        return GenericResult.Success();
    }
}
