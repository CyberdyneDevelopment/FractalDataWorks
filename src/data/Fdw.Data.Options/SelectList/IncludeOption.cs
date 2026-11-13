using System;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Options;

/// <summary>
/// Puts the row's value in the list.
/// </summary>
/// <remarks>
/// A value already in the list is not added again, and values are compared exactly (ordinal). An inactive row leaves the list as
/// it was. A row with no value fails with <c>DataOptionValueRequired</c>.
/// </remarks>
[TypeOption(typeof(SelectListOptions), "Include")]
public sealed class IncludeOption : SelectListOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncludeOption"/> class.
    /// </summary>
    public IncludeOption()
        : base("Include", 1)
    {
    }

    /// <inheritdoc />
    public override IGenericResult<DataOptionOutcome> Apply(DataOptionContext context) =>
        Change(context, (values, value) => values.Contains(value, StringComparer.Ordinal) ? values : [.. values, value]);
}
