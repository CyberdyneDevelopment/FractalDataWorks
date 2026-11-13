using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Refuses a Graph data set whose graph holds a component kind a data set graph does not allow (FDW-5287).</summary>
/// <remarks>
/// The families of refused kind, and the names in each, are the <see cref="GraphRefusedComponentKinds"/> collection: this check types no
/// kind name. Each family has its own result code, so a destination, an action and a loop each answer their own. It answers the first
/// refused component in the graph's order; the check of story 2 lists every problem with its path.
/// </remarks>
public static class GraphDataSetRefusals
{
    /// <summary>Checks the components of a Graph data set for a refused kind.</summary>
    /// <param name="graph">The Graph data set as it is to be saved.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the refusal of the first component of a refused kind.</returns>
    public static IGenericResult Check(IGraphDataSetConfiguration graph, ILogger logger)
    {
        foreach (var component in graph.Components)
        {
            var checkedKind = CheckKind(graph.Name, component.Name, component.ComponentType, logger);
            if (checkedKind.IsFailure) return checkedKind;
        }

        return GenericResult.Success();
    }

    /// <summary>Checks one component's kind, before its settings are read: a destination is refused as a destination whatever it states.</summary>
    /// <param name="dataSetName">The name of the Graph data set.</param>
    /// <param name="componentName">The name of the component.</param>
    /// <param name="componentType">The kind the component states: the name of a <c>ComponentTypes</c> option.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the refusal answering the family the kind is in.</returns>
    public static IGenericResult CheckKind(string dataSetName, string componentName, string componentType, ILogger logger)
    {
        var family = FamilyOf(componentType);
        return family is null
            ? GenericResult.Success()
            : GenericResult.Failure(
                DataServiceResultCodes.ByName(family.ResultCodeName), logger,
                ResultDetails.Create("component", componentName, "dataSet", dataSetName, "kind", componentType));
    }

    /// <summary>Finds the family of refused kind a component kind belongs to (FDW-5290): the one lookup this check and the check before save share.</summary>
    /// <param name="componentType">The component's kind: the name of a <c>ComponentTypes</c> option.</param>
    /// <returns>The family, or null when a data set graph allows the kind.</returns>
    public static IGraphRefusedComponentKind? FamilyOf(string componentType) =>
        GraphRefusedComponentKinds.All().FirstOrDefault(
            kind => kind.ComponentTypeNames.Contains(componentType, StringComparer.OrdinalIgnoreCase));
}
