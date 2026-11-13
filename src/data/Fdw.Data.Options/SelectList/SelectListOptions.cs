using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>
/// The options of the SelectList option type. Child collection of <see cref="DataOptions"/>; resolve as
/// <c>DataOptions.ByName("SelectList").ByName(optionName)</c>.
/// </summary>
/// <remarks>
/// The framework ships <c>Include</c> and <c>Exclude</c>. A package adds its own option by declaring a
/// <c>[TypeOption(typeof(SelectListOptions), "Name")]</c> against this collection from its own assembly; no change here is
/// required.
/// </remarks>
[TypeCollection(typeof(SelectListOptionBase), typeof(ISelectListOption), typeof(SelectListOptions),
    TypeOption = typeof(DataOptions), TypeOptionName = "SelectList")]
public abstract partial class SelectListOptions : TypeCollectionBase<SelectListOptionBase, ISelectListOption>
{
}
