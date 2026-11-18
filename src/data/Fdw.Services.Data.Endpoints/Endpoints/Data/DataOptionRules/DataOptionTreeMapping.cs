using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Options;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The data option tree (FDW-6342): the layers of the DataConfigurationRules root walked to any depth, with the kinds a value, an owner and a custodian state can be. It reads the
/// collections as they are registered, so a layer or kind a package adds shows with no change here, and nothing counts the depth. A node states the display name and description
/// its option states; the root is a collection, not an option, and states none.
/// </summary>
internal static class DataOptionTreeMapping
{
    /// <summary>The tree and the kinds, read from the collections.</summary>
    public static DataOptionTreePayload Tree() => new()
    {
        Root = new DataOptionTreeNodePayload { Name = nameof(global::Fdw.Data.Options.DataConfigurationRules), Children = Children(global::Fdw.Data.Options.DataConfigurationRules.All().Cast<IRuleLayer>()) },
        ValueKinds = [.. DataOptionValueKinds.All().OrderBy(kind => kind.Name, StringComparer.OrdinalIgnoreCase).Select(kind => new DataOptionValueKindPayload { Name = kind.Name, DisplayName = kind.DisplayName, Member = kind.Member })],
        OwnerKinds = [.. DataOptionOwnerKinds.All().OrderBy(kind => kind.Name, StringComparer.OrdinalIgnoreCase).Select(kind => new DataOptionNamedPayload { Name = kind.Name, DisplayName = kind.DisplayName })],
        CustodianStates = [.. DataOptionCustodianStates.All().OrderBy(state => state.Name, StringComparer.OrdinalIgnoreCase)
            .Select(state => new DataOptionCustodianStatePayload { Name = state.Name, DisplayName = state.DisplayName, NeedsAttention = state.NeedsAttention })],
    };

    private static List<DataOptionTreeNodePayload> Children(IEnumerable<IRuleLayer> layers) => [.. layers.OrderBy(layer => layer.Name, StringComparer.OrdinalIgnoreCase).Select(Node)];

    private static DataOptionTreeNodePayload Node(IRuleLayer layer) => new()
    {
        Name = layer.Name,
        DisplayName = Stated(layer.DisplayName),
        Description = Stated(layer.Description),
        Children = Children(layer.Layers),
        Leaf = layer is IRuleKind kind ? new DataOptionLeafPayload { Method = kind.Method, MinValues = kind.MinValues, MaxValues = kind.MaxValues } : null,
    };

    // A text an option does not state is absent, not an empty string.
    private static string? Stated(string text) => string.IsNullOrEmpty(text) ? null : text;
}
