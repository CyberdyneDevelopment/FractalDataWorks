using System.Collections.Generic;

namespace Fdw.Data.Options;

/// <summary>
/// What applying a SelectList option produced: the values that are selected afterwards.
/// </summary>
public sealed class SelectListOutcome : DataOptionOutcome
{
    /// <summary>Initializes a new instance of the <see cref="SelectListOutcome"/> class.</summary>
    /// <param name="optionType">The name of the option type that was applied.</param>
    /// <param name="option">The name of the option that was applied.</param>
    /// <param name="values">The values selected after the option was applied, in the order they were included.</param>
    public SelectListOutcome(string optionType, string option, IReadOnlyList<string> values)
        : base(optionType, option)
    {
        Values = values;
    }

    /// <summary>Gets the values selected after the option was applied, in the order they were included.</summary>
    public IReadOnlyList<string> Values { get; }
}
