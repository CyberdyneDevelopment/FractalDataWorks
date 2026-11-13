using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A family of component kinds a Graph data set's graph refuses (FDW-5287), and the result code that names the refusal.</summary>
public interface IGraphRefusedComponentKind : ITypeOption<int, IGraphRefusedComponentKind>
{
    /// <summary>Gets the names of the <c>ComponentTypes</c> options in this family.</summary>
    IReadOnlyList<string> ComponentTypeNames { get; }

    /// <summary>Gets the name of the data service result code a refusal of a component of this family answers.</summary>
    string ResultCodeName { get; }
}
