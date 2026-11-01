using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// One component of a graph pipeline (FDW-1396): its name, its kind and the settings that kind takes. A pipeline is one list of them
/// (FDW-2041): a source, a transformation, a join, a destination, an action (set a variable, run SQL, start another pipeline, send a
/// notification) or a loop, which holds the components that name it as <see cref="ParentComponent"/>.
/// </summary>
/// <remarks>
/// <para>
/// A setting is stated only for the kinds that take it; <see cref="ComponentTypes"/> lists them. A setting a kind requires and
/// the component leaves out is refused when the graph is built, naming the component and the setting. Nothing is assumed.
/// </para>
/// <para>
/// Cascade child of the graph, in <c>pipe.PipelineComponent</c>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineComponentConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the component's name, unique within the graph.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind, a <see cref="ComponentTypes"/> option name.</summary>
    public string ComponentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the loop component whose body this component is in; null for a top-level component (FDW-2041).</summary>
    public string? ParentComponent { get; set; }

    /// <summary>Gets or sets the data set a source reads or a destination loads.</summary>
    public string? DataSetName { get; set; }

    /// <summary>Gets or sets the data store a container source reads or a container destination loads.</summary>
    public string? DataStoreName { get; set; }

    /// <summary>Gets or sets the path in the data store; none when the store states none.</summary>
    public string? PathName { get; set; }

    /// <summary>Gets or sets the container a container source reads or a container destination loads.</summary>
    public string? ContainerName { get; set; }

    /// <summary>Gets or sets the physical path a container source reads instead of the declared container's path (FDW-1952); none reads the declared path.</summary>
    public string? PhysicalPath { get; set; }

    /// <summary>Gets or sets the physical name (the file) a container source reads instead of the declared container's name (FDW-1952); none reads the declared name.</summary>
    public string? PhysicalName { get; set; }

    /// <summary>Gets or sets the name of the <c>TargetKinds</c> option that says what a container source reads (FDW-3089); none reads the declared container as before.</summary>
    public string? TargetKind { get; set; }

    /// <summary>Gets or sets the name of the list run parameter that holds the file names of the <c>Files</c> kind (FDW-3089).</summary>
    public string? FilesList { get; set; }

    /// <summary>Gets or sets the field whose value names the file each row of the <c>Files</c> kind of a destination goes to (FDW-3193).</summary>
    public string? SplitField { get; set; }

    /// <summary>Gets or sets how many rows go in a batch.</summary>
    public int? BatchSize { get; set; }

    /// <summary>Gets or sets how a destination writes a batch, a <c>LoadStepKinds</c> option name.</summary>
    public string? LoadKind { get; set; }

    /// <summary>Gets or sets whether a destination empties its target once before the first batch.</summary>
    public bool? TruncateFirst { get; set; }

    /// <summary>Gets or sets which rows a join keeps with no partner, a <c>JoinKinds</c> option name.</summary>
    public string? JoinKind { get; set; }

    /// <summary>Gets or sets what is put before each right field name in a join's output.</summary>
    public string? RightPrefix { get; set; }

    /// <summary>Gets or sets where a row that meets several filters goes, a <c>SplitRoutings</c> option name.</summary>
    public string? Routing { get; set; }

    /// <summary>Gets or sets the name of the output node that takes unmatched rows; none means an unmatched row fails the run.</summary>
    public string? UnmatchedOutput { get; set; }

    /// <summary>Gets or sets the name of the error output node; none means a row that cannot be processed fails the run.</summary>
    public string? ErrorOutput { get; set; }

    /// <summary>Gets or sets the field a cross apply expands.</summary>
    public string? FieldName { get; set; }

    /// <summary>Gets or sets the output field a cross apply writes each value to.</summary>
    public string? TargetName { get; set; }

    /// <summary>Gets or sets the delimiter of a delimited field; none means the field holds a list.</summary>
    public string? Delimiter { get; set; }

    /// <summary>Gets or sets whether a cross apply keeps a row whose field yields no value.</summary>
    public bool? KeepsEmpty { get; set; }

    /// <summary>Gets or sets the script a SQL source reads (FDW-1443): text a data engineer wrote, with its values as named parameters.</summary>
    public string? ScriptText { get; set; }

    /// <summary>Gets or sets the most rows a SQL source's script may select; the read fails when it selects more.</summary>
    public int? MaxRows { get; set; }

    /// <summary>Gets or sets the variable a loop declares each iteration, or a Set variable component sets (FDW-1403).</summary>
    public string? VariableName { get; set; }

    /// <summary>Gets or sets a For loop's first value.</summary>
    public int? StartValue { get; set; }

    /// <summary>Gets or sets a For loop's last value, inclusive.</summary>
    public int? EndValue { get; set; }

    /// <summary>Gets or sets a For loop's increment.</summary>
    public int? StepValue { get; set; }

    /// <summary>Gets or sets the variable a For each loop reads its list from.</summary>
    public string? ListVariable { get; set; }

    /// <summary>Gets or sets a Do while loop's condition, in the filter expression language.</summary>
    public string? ConditionExpression { get; set; }

    /// <summary>Gets or sets the most times a Do while loop's body may run.</summary>
    public int? MaxIterations { get; set; }

    /// <summary>Gets or sets the <c>DataTypes</c> option name of the value a Set variable component sets.</summary>
    public string? ConstantType { get; set; }

    /// <summary>Gets or sets the value a Set variable component works from, as text; null sets the variable to null.</summary>
    public string? ConstantText { get; set; }

    /// <summary>Gets or sets the name of the pipeline an Execute pipeline component starts.</summary>
    public string? TargetPipeline { get; set; }

    /// <summary>Gets or sets whether an Execute pipeline component waits for the pipeline it started to end.</summary>
    public bool? AwaitCompletion { get; set; }

    /// <summary>Gets or sets the variable an Execute SQL or Execute pipeline component sets to its outcome.</summary>
    public string? ResultVariable { get; set; }

    /// <summary>Gets or sets whether an Execute pipeline component fails when the started pipeline does not succeed.</summary>
    public bool? FailOnChildFailure { get; set; }

    /// <summary>Gets or sets the notification rule a Notify component sends through.</summary>
    public string? NotificationRuleName { get; set; }

    /// <summary>Gets or sets the run point a Notify component fires at: <c>AfterStage</c>, <c>AfterLoad</c> or <c>OnFailure</c>.</summary>
    public string? RunPoint { get; set; }

    /// <summary>Gets or sets the message a Notify component sends, a template over the run's facts.</summary>
    public string? NotificationMessage { get; set; }

    /// <summary>Gets or sets the name of the transformation a Set variable component applies to its input (FDW-1957).</summary>
    public string? TransformationName { get; set; }

    /// <summary>Gets or sets the stage the component runs in (FDW-2040), a number from 1; none runs before every stage. When every component of a stage has finished, the next higher stage runs.</summary>
    public int? Stage { get; set; }

    /// <summary>Gets or sets the column that takes the time a version starts (FDW-1636, the destination options of the old load step).</summary>
    public string? EffectiveFromColumn { get; set; }

    /// <summary>Gets or sets the column that takes the time a version ends (FDW-1636, the destination options of the old load step).</summary>
    public string? EffectiveToColumn { get; set; }

    /// <summary>Gets or sets the column that flags the current version (FDW-1636, the destination options of the old load step).</summary>
    public string? CurrentFlagColumn { get; set; }

    /// <summary>Gets or sets the column an insert-only rule tests (FDW-1636, the destination options of the old load step).</summary>
    public string? InsertOnlyRuleColumn { get; set; }

    /// <summary>Gets or sets the value an insert-only rule looks for (FDW-1636, the destination options of the old load step).</summary>
    public string? InsertOnlyRuleValue { get; set; }

    /// <summary>Gets or sets how a soft delete is recorded, a <c>SoftDeleteModes</c> option name (FDW-1636, the destination options of the old load step).</summary>
    public string? SoftDeleteMode { get; set; }

    /// <summary>Gets or sets the column that flags a deleted row (FDW-1636, the destination options of the old load step).</summary>
    public string? DeletedFlagColumn { get; set; }

    /// <summary>Gets or sets the column that takes the time a row was deleted (FDW-1636, the destination options of the old load step).</summary>
    public string? DeletedAtColumn { get; set; }

    /// <summary>Gets or sets the column a marker mode tests (FDW-1636, the destination options of the old load step).</summary>
    public string? SoftDeleteMarkerColumn { get; set; }

    /// <summary>Gets or sets the value a marker mode looks for (FDW-1636, the destination options of the old load step).</summary>
    public string? SoftDeleteMarkerValue { get; set; }

    /// <summary>Gets or sets the horizontal position the console draws the component at; the engine never reads it.</summary>
    public int? LayoutX { get; set; }

    /// <summary>Gets or sets the vertical position the console draws the component at; the engine never reads it.</summary>
    public int? LayoutY { get; set; }

    /// <summary>Gets or sets the field mappings of the component's list settings. Cascade children (<c>pipe.PipelineComponentField</c>).</summary>
    public IList<PipelineComponentFieldConfiguration> Fields { get; set; } = [];

    /// <summary>Gets or sets the column lists of the component's list settings. Cascade children (<c>pipe.PipelineComponentColumn</c>).</summary>
    public IList<PipelineComponentColumnConfiguration> Columns { get; set; } = [];

    /// <summary>Gets or sets the values a destination supplies for target columns (FDW-1636). Cascade children (<c>pipe.PipelineComponentValueSource</c>).</summary>
    public IList<PipelineComponentValueSourceConfiguration> ValueSources { get; set; } = [];

    /// <summary>Gets or sets the settings that take their value from a run parameter (FDW-1722). Cascade children (<c>pipe.PipelineComponentSettingBinding</c>).</summary>
    public IList<PipelineComponentSettingBindingConfiguration> SettingBindings { get; set; } = [];

    /// <summary>Gets or sets a split's filtered outputs. Cascade children (<c>pipe.PipelineComponentSplitOutput</c>).</summary>
    public IList<PipelineComponentSplitOutputConfiguration> SplitOutputs { get; set; } = [];

    /// <summary>Gets or sets the values an Execute SQL, Execute pipeline or Set variable component gives its parameters (FDW-2041, was the step's). Cascade children (<c>pipe.PipelineComponentParameter</c>).</summary>
    public IList<PipelineComponentParameterConfiguration> Parameters { get; set; } = [];

    /// <summary>
    /// Gets or sets the stage a component of kind <c>Stage</c> applies, one at most: an existing stage kind (a Filter, Sort,
    /// Aggregate and so on) held as a <c>pipe.PipelineTransform</c> row that the component owns. Empty on every other kind.
    /// </summary>
    public IList<PipelineTransformConfiguration> Stages { get; set; } = [];
}
