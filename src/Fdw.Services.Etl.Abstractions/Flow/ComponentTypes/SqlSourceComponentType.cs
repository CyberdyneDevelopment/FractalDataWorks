using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Reads the rows a SQL script selects (FDW-1443): a query a data engineer wrote, run once on a data store.</summary>
[TypeOption(typeof(ComponentTypes), "SqlSource")]
public sealed class SqlSourceComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlSourceComponentType"/> class.</summary>
    public SqlSourceComponentType() : base(
        "SqlSource",
        "SQL source",
        "Reads the rows a SQL script selects; only a data engineer may write or run one.",
        "Source",
        [],
        [
            new FlowNodeSpec("output", false, 1, 1, false, "Every row the script selects."),
        ],
        [
            new StageMember("dataStore", "DataStore", true, "The data store the script runs on.", TakesParameterText: true),
            new StageMember("script", "SqlText", true, "The query; its values are named parameters such as @Year, never part of the text. A name that changes (a schema, table or column) is written $(Name) and taken from a parameter or variable whose value is letters, digits, underscore and dot only."),
            new StageMember("parameters", "FieldMaps", false, "The values of the script's named parameters: each names a parameter and gives a typed constant."),
            new StageMember("maxRows", "Number", true, "The most rows the script may select; the run fails when it selects more."),
            new StageMember("batchSize", "Number", true, "How many rows go in a batch; at least one."),
        ])
    {
    }
}
