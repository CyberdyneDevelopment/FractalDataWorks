using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Reads the data configuration rules of a column of a data container and keeps the list of values they pick in a variable (FDW-5347, FDW-6344).
/// </summary>
[TypeOption(typeof(ComponentTypes), "ReadDataOptions")]
public sealed class ReadDataOptionsComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ReadDataOptionsComponentType"/> class.</summary>
    public ReadDataOptionsComponentType() : base(
        "ReadDataOptions",
        "Read data options",
        "Reads the current, active rules of a column of a data container and keeps the list of values they pick in a variable: a rule of kind In puts its values in the list and a rule of kind NotIn takes them out, which a For each can take in batches and a source's path or a script can name as \"A\",\"B\",\"C\"; only a data engineer may run one, because it reads through the ETL gateway.",
        "Control flow",
        false,
        [
            new StageMember("optionsDataStore", "Text", true, "The name of the data store that holds the rule tables."),
            new StageMember("optionsContainer", "Text", true, "The table that holds the data configuration rules, as it is named in the data store (schema and name), for example data.DataConfigurationRule. Its name is letters, digits, underscore and dot only."),
            new StageMember("optionValuesContainer", "Text", true, "The table that holds the values of the rules, as it is named in the data store (schema and name), for example data.DataConfigurationRuleValue. Its name is letters, digits, underscore and dot only."),
            new StageMember("dataStore", "Text", true, "The name of the data store the rules are about.", SummarisesComponent: true),
            new StageMember("path", "Text", true, "The data path of the container the rules are about, as the rule states it (for example dbo).", SummarisesComponent: true),
            new StageMember("container", "Text", true, "The name of the data container the rules are about.", SummarisesComponent: true),
            new StageMember("field", "Text", true, "The name of the column the rules are about.", SummarisesComponent: true),
            new StageMember("maxRows", "Number", true, "The most value rows the rules of the column may have; the step fails when they have more."),
            new StageMember("resultVariable", "Variable", true, "The declared variable the list of values is kept in."),
        ])
    {
        // Why " . ": the card says what the step reads as "Store . Container . Column", the place the options are about.
        SummarySeparator = " . ";
    }
}
