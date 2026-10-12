using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The chain of views above a view, and the stored filters they put on it, parent first (FDW-1032).
/// </summary>
/// <remarks>
/// A view of a view reads the root data set, narrowed by its own filter and by each parent's. The chain is read upward
/// from the view's <c>ParentVariationId</c>. No endpoint changes a view's parent after it is created, and a parent exists before its child, so a chain cannot
/// loop; the depth is capped all the same, so a chain that somehow is too long is refused rather than followed. The
/// read service walks it, so every way of reading a view gets the same filters; the query that applies them is the
/// caller's, because a stored filter is read into the caller's own filter type.
/// </remarks>
public static class DataSetVariationChain
{
    /// <summary>The most views one chain may hold, the view itself included.</summary>
    public const int MaxDepth = 8;

    /// <summary>The views above a view, the root-most first.</summary>
    /// <param name="variations">The variation provider.</param>
    /// <param name="parentId">The logical id of the view's parent, or null when its parent is the data set.</param>
    /// <param name="viewName">The view's name, for a refusal.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The ancestors, or <c>DataSetVariationInvalid</c>.</returns>
    public static async Task<IGenericResult<IReadOnlyList<IDataSetVariationImplementationConfiguration>>> Ancestors(
        IDataSetVariationConfigurationProvider variations, Guid? parentId, string viewName, ILogger logger, CancellationToken ct)
    {
        var chain = new List<IDataSetVariationImplementationConfiguration>();
        var next = parentId;
        while (next is { } id)
        {
            if (chain.Count + 1 >= MaxDepth)
                return Invalid<IReadOnlyList<IDataSetVariationImplementationConfiguration>>(logger, viewName, $"it has more than {MaxDepth - 1} views above it");

            var parent = await variations.Get(id, ct).ConfigureAwait(false);
            if (parent.IsFailure) return parent.ToNewResult<IReadOnlyList<IDataSetVariationImplementationConfiguration>>();
            if (parent.Value is not { } link)
                return Invalid<IReadOnlyList<IDataSetVariationImplementationConfiguration>>(logger, viewName, "its parent view no longer exists");

            chain.Insert(0, link);
            next = link.ParentVariationId;
        }

        return GenericResult<IReadOnlyList<IDataSetVariationImplementationConfiguration>>.Success(chain);
    }

    /// <summary>The stored filter of every view in a view's chain, parent first, the view's own last.</summary>
    /// <param name="variations">The variation provider.</param>
    /// <param name="view">The view.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// The stored filter texts of the views that have one; empty when none does, which is true of the chain and not a
    /// missing value. <c>DataSetVariationInvalid</c> when the chain is broken or too long.
    /// </returns>
    public static async Task<IGenericResult<IReadOnlyList<string>>> StoredFilters(
        IDataSetVariationConfigurationProvider variations, DataSetVariationDomainConfiguration view, ILogger logger, CancellationToken ct)
    {
        var ancestors = await Ancestors(variations, view.ParentVariationId, view.VariationName, logger, ct).ConfigureAwait(false);
        if (ancestors.IsFailure) return ancestors.ToNewResult<IReadOnlyList<string>>();

        return GenericResult<IReadOnlyList<string>>.Success(
            ancestors.Value!.Select(a => a.Filters).Append(view.Filters)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(text => text!)
                .ToList());
    }

    /// <summary>A refusal that a view's chain or stored filter cannot be used.</summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="viewName">The view's name.</param>
    /// <param name="reason">Why it cannot be used.</param>
    /// <typeparam name="T">The result's value type.</typeparam>
    /// <returns>A <c>DataSetVariationInvalid</c> failure.</returns>
    public static IGenericResult<T> Invalid<T>(ILogger logger, string viewName, string reason)
        => GenericResult<T>.Failure(
            WorkspacesResultCodes.ByName("DataSetVariationInvalid"), logger,
            ResultDetails.Create("name", viewName, "reason", reason));
}
