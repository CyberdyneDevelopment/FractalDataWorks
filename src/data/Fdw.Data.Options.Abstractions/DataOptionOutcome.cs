namespace Fdw.Data.Options;

/// <summary>
/// What applying an option produced, without saying what kind of thing it is: a caller holds only the strings it read from a
/// row, so the outcome is not generic on its type. Each option type declares a subtype that carries its own result, for example
/// <c>SelectListOutcome</c>.
/// </summary>
public abstract class DataOptionOutcome
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionOutcome"/> class.</summary>
    /// <param name="optionType">The name of the option type that was applied.</param>
    /// <param name="option">The name of the option that was applied.</param>
    protected DataOptionOutcome(string optionType, string option)
    {
        OptionType = optionType;
        Option = option;
    }

    /// <summary>Gets the name of the option type that was applied.</summary>
    public string OptionType { get; }

    /// <summary>Gets the name of the option that was applied.</summary>
    public string Option { get; }
}
