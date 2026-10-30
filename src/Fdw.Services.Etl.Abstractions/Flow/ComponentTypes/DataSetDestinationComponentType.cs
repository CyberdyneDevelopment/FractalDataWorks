using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Loads the rows it receives into a data set.</summary>
[TypeOption(typeof(ComponentTypes), "DataSetDestination")]
public sealed class DataSetDestinationComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetDestinationComponentType"/> class.</summary>
    public DataSetDestinationComponentType() : base(
        "DataSetDestination",
        "Data set destination",
        "Loads the rows it receives into a data set.",
        "Destination",
        [
            new FlowNodeSpec("input", false, 1, 1, false, "The rows to load."),
        ],
        [],
        [
            new StageMember("dataSet", "DataSet", true, "The data set to load.", TakesParameterText: true),
            new StageMember("targetKind", "TargetKinds", false, "What the destination writes: DataSet (the container of the data set), File (one file, named by physicalName) or Files (one file for each value of splitField); none writes as before."),
            new StageMember("physicalPath", "Text", false, "For the File and Files kinds: the path to write at in place of the declared container's.", TakesParameterText: true, ReadsWhenStepRuns: true),
            new StageMember("physicalName", "Text", false, "For the File kind: the name (the file) to write in place of the declared container's.", TakesParameterText: true, ReadsWhenStepRuns: true),
            new StageMember("splitField", "Text", false, "For the Files kind: the field whose value names the file each row goes to; one file is written for each value, and each holds only that value's rows."),
            new StageMember("loadKind", "LoadStepKinds", true, "How a batch is written: insert, upsert and the others a load step can be."),
            new StageMember("truncateFirst", "Boolean", true, "Whether the target is emptied once, before the first batch is written."),
            new StageMember("keyColumns", "ColumnList", false, "The destination columns that match a row; the load kind says whether it needs them."),
            new StageMember("compareColumns", "ColumnList", false, "The columns that make a matched row count as changed; the load kind says whether it takes them."),
            new StageMember("writeColumns", "ColumnList", false, "The destination columns written; the load kind says whether it needs them."),
            new StageMember("effectiveFromColumn", "Text", false, "Versioning: the column that takes the time a version starts; the load kind says whether it allows versioning."),
            new StageMember("effectiveToColumn", "Text", false, "Versioning: the column that takes the time a version ends; stated with the effective-from column."),
            new StageMember("currentFlagColumn", "Text", false, "Versioning: the column that flags the current version."),
            new StageMember("insertOnlyRuleColumn", "Text", false, "Versioning: the column whose value makes a matched row insert-only; stated with its value."),
            new StageMember("insertOnlyRuleValue", "Text", false, "Versioning: the value of the insert-only rule column."),
            new StageMember("softDeleteMode", "SoftDeleteModes", false, "Soft delete: how a row that is gone from the source is recorded; the load kind says whether it allows a soft delete."),
            new StageMember("deletedFlagColumn", "Text", false, "Soft delete: the column that flags a deleted row."),
            new StageMember("deletedAtColumn", "Text", false, "Soft delete: the column that takes the time a row was deleted."),
            new StageMember("softDeleteMarkerColumn", "Text", false, "Soft delete: the column a marker mode tests; stated with its value."),
            new StageMember("softDeleteMarkerValue", "Text", false, "Soft delete: the value of the marker column."),
            new StageMember("valueSources", "ValueSources", false, "Values written to target columns: a source field, the next value of a sequence, the current time or a constant."),
        ])
    {
    }
}
