using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Runs a SQL command a data engineer wrote on a data store and optionally keeps how many rows it affected (FDW-1443).</summary>
[TypeOption(typeof(ComponentTypes), "ExecuteSql")]
public sealed class ExecuteSqlComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteSqlComponentType"/> class.</summary>
    public ExecuteSqlComponentType() : base(
        "ExecuteSql",
        "Execute SQL",
        "Runs a SQL command on a data store, with named parameters; only a data engineer may write or run one.",
        "Control flow",
        false,
        [
            new StageMember("dataStore", "Text", true, "The name of the data store the command runs on."),
            new StageMember("script", "SqlText", true, "The command; its values are named parameters such as @Year, never part of the text. A name that changes (a schema, table or column) is written $(Name) and taken from a parameter or variable whose value is letters, digits, underscore and dot only."),
            new StageMember("parameters", "Parameters", false, "The values of its parameters, as text; a value may name a variable in double braces."),
            new StageMember("resultVariable", "Variable", false, "A declared Int32 variable set to the rows the command affected."),
        ])
    {
    }
}
