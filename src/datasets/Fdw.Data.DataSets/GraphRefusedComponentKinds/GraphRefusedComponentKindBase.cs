using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The base of the families of component kind a Graph data set's graph refuses.</summary>
public abstract class GraphRefusedComponentKindBase : TypeOptionBase<GraphRefusedComponentKindBase>, IGraphRefusedComponentKind
{
    /// <summary>Initializes a new instance of the <see cref="GraphRefusedComponentKindBase"/> class.</summary>
    /// <param name="name">The family's name.</param>
    /// <param name="description">Why a data set graph refuses the family.</param>
    /// <param name="componentTypeNames">The names of the <c>ComponentTypes</c> options in the family.</param>
    /// <param name="resultCodeName">The name of the data service result code its refusal answers.</param>
    protected GraphRefusedComponentKindBase(string name, string description, IReadOnlyList<string> componentTypeNames, string resultCodeName)
        : base(name, name, description, "GraphRefusedComponentKinds")
    {
        ComponentTypeNames = componentTypeNames;
        ResultCodeName = resultCodeName;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ComponentTypeNames { get; }

    /// <inheritdoc />
    public string ResultCodeName { get; }
}
