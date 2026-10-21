namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// One member of a pipeline stage's body that a stage kind owns, as the type-collection listing describes it.
/// </summary>
/// <param name="Name">The member's name in the body, in camel case.</param>
/// <param name="Kind">What the member holds, for the console's picker (for example <c>Expression</c> or <c>CheckList</c>).</param>
/// <param name="IsRequired">Whether a stage of the kind must state the member.</param>
/// <param name="HelpText">A short explanation for the person filling it in.</param>
/// <param name="ShapesGraph">
/// Whether links, other components or kept rows refer to what the setting names, so the graph is checked on it before any run and a run
/// cannot choose it (FDW-1768): the names of error outputs, the output of a lookup that found no match, the source a source starts after,
/// the name rows are kept under.
/// </param>
/// <param name="TakesParameterText">
/// Whether the setting reads <c>{{Name}}</c> and renders the run's parameter into it when the run starts (FDW-1689): the data set, data
/// store, path and container a source or destination reads or writes.
/// </param>
/// <param name="TakesPairList">
/// Whether a run may supply the whole setting as a list parameter whose items are field map pairs, <c>Source -> Target</c> (FDW-1745):
/// the mappings on a component input. Only a setting of kind <c>FieldMaps</c> whose maps are each a source field and a target field has it.
/// </param>
/// <param name="ReadsWhenStepRuns">
/// Whether a <c>TakesParameterText</c> setting is rendered when the component's step runs, from the variables as they are then, not when the run
/// starts (FDW-1958): the physical path and name a source reads. What the run reaches is judged on the settings that are not late, so a late
/// setting may name a variable an earlier step calculated without changing what the caller is allowed to read.
/// </param>
/// <param name="TakesConversions">
/// Whether each field mapping of the setting may also state a type to convert to, a default, a required flag and a chain of named Transformations
/// (FDW-2000): the mappings on a component input. A setting of kind <c>FieldMaps</c> that does not have it refuses those parts.
/// </param>
public sealed record StageMember(
    string Name, string Kind, bool IsRequired, string HelpText, bool ShapesGraph = false, bool TakesParameterText = false, bool TakesPairList = false,
    bool ReadsWhenStepRuns = false, bool TakesConversions = false);
