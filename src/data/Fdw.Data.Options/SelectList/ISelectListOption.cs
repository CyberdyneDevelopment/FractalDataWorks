using Fdw.Collections;

namespace Fdw.Data.Options;

/// <summary>
/// A named option of the SelectList option type: what a stored row does to the list of values selected for a column.
/// </summary>
/// <remarks>
/// A member of <see cref="SelectListOptions"/>, never of the root <see cref="IDataOptionType"/> collection. The framework ships
/// <c>Include</c> and <c>Exclude</c>; a package adds its own by declaring a <c>[TypeOption(typeof(SelectListOptions), "Name")]</c>.
/// Every option of this type answers with a <see cref="SelectListOutcome"/>.
/// </remarks>
public interface ISelectListOption : ITypeOption<int, SelectListOptionBase>, IDataOption
{
    /// <summary>Gets the option's name, which a stored row names as its option.</summary>
    /// <remarks>Declared again so that <c>Name</c> is one member here: both <see cref="ITypeOption{TKey, TValue}"/> and <see cref="IDataOption"/> have it, and the generated collection reads it.</remarks>
    new string Name { get; }
}
