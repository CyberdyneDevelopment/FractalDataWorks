using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What makes a variation a data view: the one predicate that separates them from the ones a workspace owns.</summary>
/// <remarks>
/// Data views and scoped variations are rows of the same domain table, told apart by the implementation they name. Every
/// read that lists or queries data views holds itself to this predicate, so a list of data views never shows a
/// workspace's variation.
/// </remarks>
internal static class DataViewScope
{
    /// <summary>Gets the filter that keeps only data views.</summary>
    internal static FilterExpression Filter { get; } = new()
    {
        Root = new FilterCondition
        {
            PropertyName = nameof(DataSetVariationDomainConfiguration.Implementation),
            Operator = FilterOperators.ByName("Equal"),
            Value = DataViewDataSetVariationConfiguration.ImplementationName,
        },
    };
}
