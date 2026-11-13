using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>
/// Root collection of the option types a person can use to turn the values of a data container's column on or off.
/// </summary>
/// <remarks>
/// A member here is a dispatcher, not an option: <c>DataOptions.ByName("SelectList")</c> resolves to that option type's
/// <see cref="IDataOptionType"/>, and its own <see cref="IDataOptionType.ByName"/> resolves an option name against that option
/// type's child collection (<c>SelectListOptions</c>). An option type joins this collection with a
/// <c>[TypeOption(typeof(DataOptions), "TypeName")]</c> from its own package; nothing here references a specific option type.
/// The framework ships <c>SelectList</c>.
/// </remarks>
[TypeCollection(typeof(DataOptionTypeBase), typeof(IDataOptionType), typeof(DataOptions))]
public abstract partial class DataOptions : TypeCollectionBase<DataOptionTypeBase, IDataOptionType>
{
}
