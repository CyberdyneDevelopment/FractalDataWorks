using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Loads the rows it receives into one container of a data store.</summary>
[TypeOption(typeof(ComponentTypes), "ContainerDestination")]
public sealed class ContainerDestinationComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerDestinationComponentType"/> class.</summary>
    public ContainerDestinationComponentType() : base(
        "ContainerDestination",
        "Container destination",
        "Loads the rows it receives into one container of a data store.",
        "Destination",
        [
            new FlowNodeSpec("input", false, 1, 1, false, "The rows to load."),
        ],
        [],
        [
            new StageMember("dataStore", "DataStore", true, "The data store to load.", TakesParameterText: true),
            new StageMember("path", "DataStorePath", false, "The path in the data store; none when the store states none.", TakesParameterText: true),
            new StageMember("container", "Container", true, "The container to load.", TakesParameterText: true),
            new StageMember("targetKind", "TargetKinds", false, "What the destination writes: Container (the declared container), File (one file, named by physicalName) or Files (one file for each value of splitField); none writes as before."),
            new StageMember("physicalPath", "Text", false, "For the File and Files kinds: the path to write at in place of the declared container's.", TakesParameterText: true, ReadsWhenStepRuns: true),
            new StageMember("physicalName", "Text", false, "For the File kind: the name (the file) to write in place of the declared container's.", TakesParameterText: true, ReadsWhenStepRuns: true),
            new StageMember("splitField", "Text", false, "For the Files kind: the field whose value names the file each row goes to; one file is written for each value, and each holds only that value's rows."),
            new StageMember("loadKind", "LoadStepKinds", true, "How a batch is written: insert, upsert and the others a load step can be."),
            new StageMember("truncateFirst", "Boolean", true, "Whether the target is emptied once, before the first batch is written."),
            new StageMember("keyColumns", "ColumnList", false, "The destination columns that match a row; the load kind says whether it needs them."),
            new StageMember("compareColumns", "ColumnList", false, "The columns that make a matched row count as changed; the load kind says whether it takes them."),
            new StageMember("writeColumns", "ColumnList", false, "The destination columns written; the load kind says whether it needs them."),
        ])
    {
    }
}
