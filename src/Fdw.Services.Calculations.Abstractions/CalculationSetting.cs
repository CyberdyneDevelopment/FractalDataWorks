namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// One setting a calculation entity type reads from the configuration a create or update sends, or one member a calculation input
/// kind takes on an input: the name of the member, the kind a client draws it as, whether it is required, and the help text it shows.
/// </summary>
/// <param name="Name">The member, as a client sends it (camel case).</param>
/// <param name="Kind">The kind a client draws: Text, Number, Field, DataSet, Connection, or the name of a type collection the value is a choice from.</param>
/// <param name="IsRequired">Whether create refuses a configuration or an input without it.</param>
/// <param name="HelpText">What the setting means, for a form.</param>
/// <param name="AllowsEmpty">Whether an empty value is a value: a required setting that allows it is left out only when it is null; one that does not is left out when it is null, empty or blank.</param>
public sealed record CalculationSetting(string Name, string Kind, bool IsRequired, string HelpText, bool AllowsEmpty = false);
