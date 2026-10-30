using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Reads the rows of one container of a data store, a page at a time.</summary>
[TypeOption(typeof(ComponentTypes), "ContainerSource")]
public sealed class ContainerSourceComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerSourceComponentType"/> class.</summary>
    public ContainerSourceComponentType() : base(
        "ContainerSource",
        "Container source",
        "Reads the rows of one container of a data store, a page at a time.",
        "Source",
        [],
        [
            new FlowNodeSpec("output", false, 1, 1, false, "Every row of the container."),
        ],
        [
            new StageMember("dataStore", "DataStore", true, "The data store to read.", TakesParameterText: true),
            new StageMember("path", "DataStorePath", false, "The path in the data store; none when the store states none.", TakesParameterText: true),
            new StageMember("container", "Container", true, "The container to read.", TakesParameterText: true),
            new StageMember("physicalPath", "Text", false, "The path to read instead of the declared container's, for a file whose location is known only when the run starts; the declared container still gives the shape.", TakesParameterText: true, ReadsWhenStepRuns: true),
            new StageMember("physicalName", "Text", false, "The name (the file) to read instead of the declared container's, for a file whose name is known only when the run starts; the declared container still gives the shape.", TakesParameterText: true, ReadsWhenStepRuns: true),
            new StageMember("targetKind", "TargetKinds", false, "What the source reads: Container (the declared container), File (one file, named by physicalName) or Files (a list of files read as one stream, named by filesList); none reads the declared container as before."),
            new StageMember("filesList", "Text", false, "For the Files kind: the name of the list run parameter that holds the file names. The files are read one after another as one stream, and every file has the fields of the first."),
            new StageMember("batchSize", "Number", true, "How many rows go in a batch; at least one."),
        ])
    {
    }
}
