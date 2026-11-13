using Fdw.Results;

namespace Fdw.Data.Options;

/// <summary>
/// One option of an option type: what a stored row that names it does to the values of a column.
/// </summary>
/// <remarks>
/// A caller holds only the strings it read from a row, so it resolves an option as
/// <c>DataOptions.ByName(optionType).ByName(option)</c> and calls <see cref="Apply"/> without knowing which option type it
/// got. What a particular option type produces is a subtype of <see cref="DataOptionOutcome"/>.
/// </remarks>
public interface IDataOption
{
    /// <summary>Gets the option's name, which a stored row names as its option.</summary>
    string Name { get; }

    /// <summary>
    /// Gets where this option comes among the options of its type when several rows apply to one column: lower applies first,
    /// so the outcome does not depend on the order the rows were read in.
    /// </summary>
    int ApplyOrder { get; }

    /// <summary>Applies the option to a context.</summary>
    /// <param name="context">The column, the stored row and the values selected so far.</param>
    /// <returns>What applying the option produced, or the failure that says why it could not be applied. Nothing the context holds is changed.</returns>
    IGenericResult<DataOptionOutcome> Apply(DataOptionContext context);
}
