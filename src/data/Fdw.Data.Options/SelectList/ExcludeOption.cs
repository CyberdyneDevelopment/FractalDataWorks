using System;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Options;

/// <summary>
/// Takes the row's value out of the list.
/// </summary>
/// <remarks>
/// A value that is not in the list leaves it as it was, and values are compared exactly (ordinal). Exclude applies after Include,
/// so a value that is both included and excluded ends out of the list. An inactive row leaves the list as it was. A row with no
/// value fails with <c>DataOptionValueRequired</c>.
/// </remarks>
[TypeOption(typeof(SelectListOptions), "Exclude")]
public sealed class ExcludeOption : SelectListOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcludeOption"/> class.
    /// </summary>
    public ExcludeOption()
        : base("Exclude", 2)
    {
    }

    /// <inheritdoc />
    public override IGenericResult<DataOptionOutcome> Apply(DataOptionContext context) =>
        Change(context, (values, value) => [.. values.Where(held => !string.Equals(held, value, StringComparison.Ordinal))]);
}
