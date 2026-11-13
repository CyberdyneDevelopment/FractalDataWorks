using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>One component of a Graph data set's graph (FDW-5287): a source, a join, a lookup, a split, a union, a cross apply or a stage tool.</summary>
/// <remarks>
/// Cascade child of <see cref="GraphDataSetConfiguration"/>, in <c>data.GraphDataSetComponent</c>. It has the members of a pipeline's component
/// for the kinds a data set graph allows, and none for a destination, an action or a loop, which are refused by name when the graph is saved
/// (<see cref="GraphRefusedComponentKinds"/>). It is its own class and table because a pipeline's component belongs to a pipeline and an existing
/// table does not change for a new kind. <see cref="GraphDataSetLinkConfiguration"/> joins the components.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class GraphDataSetComponentConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the stable logical identifier for this component record, minted by the app before INSERT.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the component's name, unique within the graph.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the component's kind: the name of a <c>ComponentTypes</c> option. A destination, an action or a loop is refused when the graph is saved.</summary>
    public string ComponentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the data set a <c>DataSetSource</c> reads.</summary>
    public string? DataSetName { get; set; }

    /// <summary>Gets or sets the name of the data store a container or SQL source reads.</summary>
    public string? DataStoreName { get; set; }

    /// <summary>Gets or sets the path of the container a source reads.</summary>
    public string? PathName { get; set; }

    /// <summary>Gets or sets the name of the container a source reads.</summary>
    public string? ContainerName { get; set; }

    /// <summary>Gets or sets the address a container source reads in place of its declared one, as a pipeline component's does.</summary>
    public string? PhysicalPath { get; set; }

    /// <summary>Gets or sets the name a container source reads in place of its declared one, as a pipeline component's does.</summary>
    public string? PhysicalName { get; set; }

    /// <summary>Gets or sets what a <c>ContainerSource</c> reads: Container, File or Files.</summary>
    public string? TargetKind { get; set; }

    /// <summary>Gets or sets the list run parameter that holds the file names of the Files kind.</summary>
    public string? FilesList { get; set; }

    /// <summary>Gets or sets the rows a source or lookup reads at a time.</summary>
    public int? BatchSize { get; set; }

    /// <summary>Gets or sets the kind of a <c>MergeJoin</c>.</summary>
    public string? JoinKind { get; set; }

    /// <summary>Gets or sets the prefix a <c>MergeJoin</c> puts on a right field that shares a name with a left one.</summary>
    public string? RightPrefix { get; set; }

    /// <summary>Gets or sets how a <c>ConditionalSplit</c> routes a row.</summary>
    public string? Routing { get; set; }

    /// <summary>Gets or sets the output a row that matched nothing goes to.</summary>
    public string? UnmatchedOutput { get; set; }

    /// <summary>Gets or sets the output a row that failed goes to.</summary>
    public string? ErrorOutput { get; set; }

    /// <summary>Gets or sets the field a <c>CrossApply</c> splits.</summary>
    public string? FieldName { get; set; }

    /// <summary>Gets or sets the field a <c>CrossApply</c> writes each part to.</summary>
    public string? TargetName { get; set; }

    /// <summary>Gets or sets the delimiter a <c>CrossApply</c> splits on.</summary>
    public string? Delimiter { get; set; }

    /// <summary>Gets or sets whether a <c>CrossApply</c> keeps empty parts.</summary>
    public bool? KeepsEmpty { get; set; }

    /// <summary>Gets or sets the SQL a <c>SqlSource</c> runs.</summary>
    public string? ScriptText { get; set; }

    /// <summary>Gets or sets the most rows a <c>SqlSource</c> reads.</summary>
    public int? MaxRows { get; set; }

    /// <summary>Gets or sets the horizontal position the builder draws the component at; the engine never reads it.</summary>
    public int? LayoutX { get; set; }

    /// <summary>Gets or sets the vertical position the builder draws the component at; the engine never reads it.</summary>
    public int? LayoutY { get; set; }

    /// <summary>Gets or sets the field mappings of the component's list settings. Cascade children (<c>data.GraphDataSetComponentField</c>).</summary>
    public IList<GraphDataSetComponentFieldConfiguration> Fields { get; set; } = [];

    /// <summary>Gets or sets the column lists of the component's list settings. Cascade children (<c>data.GraphDataSetComponentColumn</c>).</summary>
    public IList<GraphDataSetComponentColumnConfiguration> Columns { get; set; } = [];

    /// <summary>Gets or sets a split's filtered outputs. Cascade children (<c>data.GraphDataSetComponentSplitOutput</c>).</summary>
    public IList<GraphDataSetComponentSplitOutputConfiguration> SplitOutputs { get; set; } = [];

    /// <summary>
    /// Gets or sets the stage a component of a stage tool kind (Filter, Sort, Aggregate, Calculate and so on) applies, one at most, held as a
    /// <c>data.GraphDataSetStage</c> row that the component owns. Empty on every other kind.
    /// </summary>
    public IList<GraphDataSetStageConfiguration> Stages { get; set; } = [];
}
