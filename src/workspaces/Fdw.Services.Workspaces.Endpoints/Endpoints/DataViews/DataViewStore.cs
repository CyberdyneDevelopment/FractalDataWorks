using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Finds a data view and answers the questions every endpoint in this group asks about one.</summary>
internal static class DataViewStore
{
    /// <summary>The data view under this name, or nothing when there is none.</summary>
    /// <remarks>A variation a workspace owns is not a data view, so a name that reaches one answers nothing.</remarks>
    /// <param name="variations">The variation provider.</param>
    /// <param name="name">The view's name.</param>
    /// <param name="ct">The cancellation token.</param>
    internal static async Task<IGenericResult<IDataViewDataSetVariationConfiguration?>> Find(
        IDataSetVariationConfigurationProvider variations, string name, CancellationToken ct)
    {
        var found = await variations.Get(name, ct).ConfigureAwait(false);
        return found.IsFailure
            ? found.ToNewResult<IDataViewDataSetVariationConfiguration?>()
            : GenericResult<IDataViewDataSetVariationConfiguration?>.Success(found.Value as IDataViewDataSetVariationConfiguration);
    }

    /// <summary>The name of the view this one is a view of, or null when it is a view of a data set.</summary>
    /// <param name="variations">The variation provider.</param>
    /// <param name="view">The view.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">The cancellation token.</param>
    internal static async Task<IGenericResult<string?>> ParentName(
        IDataSetVariationConfigurationProvider variations, IDataSetVariationImplementationConfiguration view, ILogger logger, CancellationToken ct)
    {
        if (view.ParentVariationId is not { } parentId) return GenericResult<string?>.Success(null);

        var parent = await variations.Get(parentId, ct).ConfigureAwait(false);
        if (parent.IsFailure) return parent.ToNewResult<string?>();

        return parent.Value is { } found
            ? GenericResult<string?>.Success(found.Name)
            : DataSetVariationChain.Invalid<string?>(logger, view.Name, "its parent view no longer exists");
    }

    /// <summary>The name of the view a domain row is a view of, or null when it is a view of a data set.</summary>
    /// <remarks>One read of the domain table by the parent's id, so a list never composes an implementation.</remarks>
    /// <param name="variations">The variation provider.</param>
    /// <param name="row">The view's domain row.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">The cancellation token.</param>
    internal static async Task<IGenericResult<string?>> ParentNameOf(
        IDataSetVariationConfigurationProvider variations, DataSetVariationDomainConfiguration row, ILogger logger, CancellationToken ct)
    {
        if (row.ParentVariationId is not { } parentId) return GenericResult<string?>.Success(null);

        var parents = await variations.Get(new[] { parentId }, ct).ConfigureAwait(false);
        if (parents.IsFailure) return parents.ToNewResult<string?>();

        return parents.Value!.Count > 0
            ? GenericResult<string?>.Success(parents.Value[0].Name)
            : DataSetVariationChain.Invalid<string?>(logger, row.Name, "its parent view no longer exists");
    }

    /// <summary>The name of a view built on this one, or nothing when none is.</summary>
    /// <remarks>One query on the variation table by parent; it reads no further than the first row.</remarks>
    /// <param name="variations">The variation provider.</param>
    /// <param name="parentId">The logical id of the view that may have views built on it.</param>
    /// <param name="ct">The cancellation token.</param>
    internal static async Task<IGenericResult<string?>> FirstDependent(
        IDataSetVariationConfigurationProvider variations, Guid parentId, CancellationToken ct)
    {
        var page = await variations.Get(
            new DomainQuery(
                new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = nameof(DataSetVariationDomainConfiguration.ParentVariationId),
                        Operator = FilterOperators.ByName("Equal"),
                        Value = parentId,
                    },
                },
                null, 0, 1, false),
            ct).ConfigureAwait(false);
        if (page.IsFailure) return page.ToNewResult<string?>();

        await foreach (var row in page.Value!.Items.WithCancellation(ct).ConfigureAwait(false))
            return GenericResult<string?>.Success(row.Name);

        return page.Value.Outcome.IsFailure
            ? page.Value.Outcome.ToNewResult<string?>()
            : GenericResult<string?>.Success(null);
    }

    /// <summary>Refuses a caller who is not the user who saved the view.</summary>
    /// <param name="authContext">The signed-in caller.</param>
    /// <param name="view">The view a change or a delete is asked of.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    internal static IGenericResult OwnedByCaller(
        IAuthenticationContextAccessor authContext, IDataViewDataSetVariationConfiguration view, ILogger logger)
    {
        if (authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var userId))
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), logger,
                ResultDetails.Create("name", view.Name, "reason", "no signed-in user"));
        }

        return view.OwnerUserId == userId
            ? GenericResult.Success()
            : GenericResult.Failure(
                WorkspacesResultCodes.ByName("DataViewWriteNotPermitted"), logger,
                ResultDetails.Create("name", view.Name, "reason", "only the user who saved it may change or delete it"));
    }
}
