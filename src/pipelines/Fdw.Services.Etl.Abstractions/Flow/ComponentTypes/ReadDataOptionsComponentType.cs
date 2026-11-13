using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Reads the data options stored for a column of a data container, applies them, and keeps the list of values they leave in a variable (FDW-5347).
/// </summary>
[TypeOption(typeof(ComponentTypes), "ReadDataOptions")]
public sealed class ReadDataOptionsComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ReadDataOptionsComponentType"/> class.</summary>
    public ReadDataOptionsComponentType() : base(
        "ReadDataOptions",
        "Read data options",
        "Reads the stored options of a column of a data container, applies each one (for example Include and Exclude of a select list), and keeps the list of values they leave in a variable, which a For each can take in batches and a source's path or a script can name as \"A\",\"B\",\"C\"; only a data engineer may run one, because it reads through the ETL gateway.",
        "Control flow",
        false,
        [
            new StageMember("optionsDataStore", "Text", true, "The name of the data store that holds the stored options."),
            new StageMember("optionsContainer", "Text", true, "The table that holds the stored options, as it is named in the data store (schema and name), one row for each option, with the fields DataStore, Container, Column, Value, OptionType, Option and Active. Its name is letters, digits, underscore and dot only."),
            new StageMember("dataStore", "Text", true, "The name of the data store the options are about.", SummarisesComponent: true),
            new StageMember("container", "Text", true, "The name of the data container the options are about.", SummarisesComponent: true),
            new StageMember("field", "Text", true, "The name of the column the options are about.", SummarisesComponent: true),
            new StageMember("maxRows", "Number", true, "The most option rows the column may have; the step fails when it has more."),
            new StageMember("resultVariable", "Variable", true, "The declared variable the list of values is kept in."),
        ])
    {
        // Why " . ": the card says what the step reads as "Store . Container . Column", the place the options are about.
        SummarySeparator = " . ";
    }
}
