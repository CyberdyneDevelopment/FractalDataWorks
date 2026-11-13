using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.Options.Logging;
using Fdw.Data.Options.Results;
using Fdw.Results;

namespace Fdw.Data.Options;

/// <summary>
/// Base class for the options of <see cref="SelectListOptions"/>.
/// </summary>
public abstract class SelectListOptionBase : TypeOptionBase<SelectListOptionBase>, ISelectListOption
{
    /// <summary>Initializes a new instance of the <see cref="SelectListOptionBase"/> class.</summary>
    /// <param name="name">The option name, which a stored row names as its option.</param>
    /// <param name="applyOrder">Where the option comes among the options of the SelectList type; lower applies first.</param>
    /// <remarks>
    /// Also the constructor the generator calls to build the collection's NotFound sentinel, with a synthesized placeholder name.
    /// </remarks>
    protected SelectListOptionBase(string name, int applyOrder)
        : base(name)
    {
        ApplyOrder = applyOrder;
    }

    /// <inheritdoc />
    public int ApplyOrder { get; }

    /// <inheritdoc />
    /// <remarks>
    /// The base body fails with <c>DataOptionNotFound</c>: it is what the collection's NotFound sentinel, a subclass with no
    /// overrides, answers when a stored row names an option the type does not have. A real option overrides it.
    /// </remarks>
    public virtual IGenericResult<DataOptionOutcome> Apply(DataOptionContext context)
    {
        DataOptionLog.OptionNotFound(context.Logger, context.OptionType, context.Option);
        return GenericResult<DataOptionOutcome>.Failure(
            DataOptionResultCodes.ByName("DataOptionNotFound"),
            ResultDetails.Create("OptionType", context.OptionType, "Option", context.Option));
    }

    /// <summary>
    /// Applies a change to the selected values when the row is active and has a value.
    /// </summary>
    /// <param name="context">The column, the stored row and the values selected so far.</param>
    /// <param name="change">Makes the values selected afterwards from the ones selected so far and the row's value.</param>
    /// <returns>
    /// The outcome with the changed values; the same values when the row is inactive; or the failure that says the row has no
    /// value. Nothing the context holds is changed.
    /// </returns>
    protected IGenericResult<DataOptionOutcome> Change(DataOptionContext context, Func<IReadOnlyList<string>, string, IReadOnlyList<string>> change)
    {
        DataOptionLog.Applying(context.Logger, context.OptionType, context.Option, context.ColumnName, context.ContainerName, context.DataStoreName);
        if (!context.IsActive)
        {
            return Outcome(context, context.Values);
        }

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            DataOptionLog.ValueRequired(context.Logger, context.OptionType, context.Option, context.ColumnName, context.ContainerName);
            return GenericResult<DataOptionOutcome>.Failure(
                DataOptionResultCodes.ByName("DataOptionValueRequired"),
                ResultDetails.Create("OptionType", context.OptionType, "Option", context.Option, "Column", context.ColumnName).With("Container", context.ContainerName));
        }

        return Outcome(context, change(context.Values, context.Value));
    }

    private IGenericResult<DataOptionOutcome> Outcome(DataOptionContext context, IReadOnlyList<string> values)
    {
        DataOptionLog.Applied(context.Logger, context.OptionType, Name, context.ColumnName, values.Count);
        return GenericResult<DataOptionOutcome>.Success(new SelectListOutcome(context.OptionType, Name, values));
    }
}
