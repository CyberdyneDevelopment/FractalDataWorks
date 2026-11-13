using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>
/// SelectList's entry point into the <see cref="DataOptions"/> root collection: the option type that decides which values of a
/// column are in a list.
/// </summary>
[TypeOption(typeof(DataOptions), "SelectList")]
public sealed class SelectListDataOptionType : DataOptionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectListDataOptionType"/> class.
    /// </summary>
    public SelectListDataOptionType()
        : base("SelectList")
    {
    }

    /// <inheritdoc />
    public override IDataOption ByName(string optionName) => SelectListOptions.ByName(optionName);

    /// <inheritdoc />
    public override IReadOnlyList<IDataOption> Options => [.. SelectListOptions.All()];
}
