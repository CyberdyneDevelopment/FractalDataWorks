using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Quality;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Retires a category (FDW-1465). A category with categories, data sets or glossary terms under it stays.</summary>
public abstract class DeleteCategoryEndpointBase : CrudDeleteEndpointBase<CategoryNameRequest>
{
    private readonly CategoryConfigurationProvider _categories;
    private readonly DataSetConfigurationProvider _dataSets;
    private readonly IGlossaryTermConfigurationProvider _terms;

    /// <summary>Initializes a new instance of the <see cref="DeleteCategoryEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categories">The categories.</param>
    /// <param name="dataSets">The data sets, which cite the categories they sit under.</param>
    /// <param name="terms">The glossary terms, which cite the categories they sit under.</param>
    protected DeleteCategoryEndpointBase(
        ILogger<DeleteCategoryEndpointBase> logger,
        CategoryConfigurationProvider categories,
        DataSetConfigurationProvider dataSets,
        IGlossaryTermConfigurationProvider terms)
        : base(logger)
    {
        _categories = categories;
        _dataSets = dataSets;
        _terms = terms;
    }

    /// <inheritdoc />
    protected override string ResourceName => "categories";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(CategoryNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(CategoryNameRequest request, CancellationToken ct)
    {
        var existing = await _categories.Get(request.Name, ct).ConfigureAwait(false);
        return existing.IsFailure ? existing.ToNewResult<bool>() : GenericResult<bool>.Success(existing.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Delete(CategoryNameRequest request, CancellationToken ct)
    {
        var tree = await CategoryTrees.Load(_categories, Logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null)
        {
            return tree;
        }

        if (tree.Value.IsAmbiguous(request.Name))
        {
            return CategoryRefusal.Of<bool>($"'{request.Name}' is a category of more than one tenant or organisation, so the name alone does not say which");
        }

        if (tree.Value.FindByName(request.Name) is not { } node)
        {
            return GenericResult.Success();
        }

        if (node.Children.Count > 0)
        {
            return GenericResult.Failure(
                DataSetsResultCodes.ByName("CategoryInUse"), Logger,
                ResultDetails.Create(
                    "category", request.Name,
                    "reason", string.Create(CultureInfo.InvariantCulture, $"{node.Children.Count} categor(ies) sit under it")));
        }

        var citing = await DataSetCategoryLinks.Citing(_dataSets, [node.Node.Id], ct).ConfigureAwait(false);
        if (citing.IsFailure || citing.Value is null)
        {
            return citing;
        }

        if (citing.Value.Count > 0)
        {
            return GenericResult.Failure(
                DataSetsResultCodes.ByName("CategoryInUse"), Logger,
                ResultDetails.Create(
                    "category", request.Name,
                    "reason", string.Create(CultureInfo.InvariantCulture, $"{citing.Value.Count} data set(s) sit under it")));
        }

        var terms = await _terms.Get(ct).ConfigureAwait(false);
        if (terms.IsFailure || terms.Value is null)
        {
            return terms;
        }

        var citedTerms = terms.Value.Count(term => term.CategoryId == node.Node.Id);
        if (citedTerms > 0)
        {
            return GenericResult.Failure(
                DataSetsResultCodes.ByName("CategoryInUse"), Logger,
                ResultDetails.Create(
                    "category", request.Name,
                    "reason", string.Create(CultureInfo.InvariantCulture, $"{citedTerms} glossary term(s) sit under it")));
        }

        CategoryEndpointLog.Retiring(Logger, request.Name);
        return await _categories.Delete(node.Node.Id, ct).ConfigureAwait(false);
    }
}
