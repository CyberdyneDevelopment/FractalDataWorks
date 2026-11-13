using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The Graph kind of data set (FDW-5286): a data set defined by a graph of row components that ends in its fields out.</summary>
/// <remarks>
/// A reader that asks for <see cref="IDataSetImplementationConfiguration.Fields"/> gets the fields out as the shared field type; the
/// stored rows are <see cref="GraphFields"/>. Nothing is stored for the rows themselves: reading the data set runs the graph.
/// </remarks>
public interface IGraphDataSetConfiguration : IDataSetImplementationConfiguration
{
    /// <summary>Gets or sets the name of the component that carries the fields out, which a read runs to; null while the data set is abstract.</summary>
    string? FieldsOutComponent { get; set; }

    /// <summary>Gets or sets the name of the output node of <see cref="FieldsOutComponent"/> that carries the fields out; null while the data set is abstract.</summary>
    string? FieldsOutOutput { get; set; }

    /// <summary>Gets or sets the fields out as they are stored, one row per field in <c>data.GraphDataSetField</c>.</summary>
    IList<GraphDataSetFieldConfiguration> GraphFields { get; set; }

    /// <summary>Gets or sets the components of the graph, one row per component in <c>data.GraphDataSetComponent</c>; none while the data set is abstract.</summary>
    IList<GraphDataSetComponentConfiguration> Components { get; set; }

    /// <summary>Gets or sets the links of the graph, one row per link in <c>data.GraphDataSetLink</c>; none while the data set is abstract.</summary>
    IList<GraphDataSetLinkConfiguration> Links { get; set; }

    /// <summary>Gets or sets the intended sources, one row per intent in <c>data.GraphDataSetIntent</c> (FDW-5294); empty once the data set is bound.</summary>
    IList<GraphDataSetIntentConfiguration> Intents { get; set; }
}
