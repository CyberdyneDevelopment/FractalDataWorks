using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Conventions;
using Fdw.Data.Options.Logging;
using Fdw.Data.Options.Results;
using Fdw.Results;

namespace Fdw.Data.Options;

/// <summary>
/// Base class for an option type's root entry ("SelectList", ...) in <see cref="DataOptions"/>.
/// </summary>
public abstract class DataOptionTypeBase : TypeOptionBase<DataOptionTypeBase>, IDataOptionType
{
    private static readonly IDataOption Unregistered = new UnregisteredDataOption();

    /// <summary>Initializes a new instance of the <see cref="DataOptionTypeBase"/> class.</summary>
    /// <param name="name">The option type name, for example <c>"SelectList"</c>.</param>
    /// <remarks>
    /// Also the constructor the generator calls to build the collection's NotFound sentinel, with a synthesized placeholder
    /// name; it needs no separate parameterless overload.
    /// </remarks>
    protected DataOptionTypeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// The base body has no child collection to delegate to, so it returns an option whose <see cref="IDataOption.Apply"/> fails
    /// with <c>DataOptionTypeNotFound</c>. A real option type overrides it to delegate into its own collection. The base body
    /// exists so the NotFound sentinel, a subclass with no overrides, still answers with a failure and not a null.
    /// </remarks>
    public virtual IDataOption ByName(string optionName) => Unregistered;

    /// <inheritdoc />
    public virtual IReadOnlyList<IDataOption> Options => [];

    private sealed class UnregisteredDataOption : IDataOption
    {
        public string Name => DataOptionPlaceholders.UnregisteredName;

        public int ApplyOrder => 0;

        public IGenericResult<DataOptionOutcome> Apply(DataOptionContext context)
        {
            DataOptionLog.OptionTypeNotFound(context.Logger, context.OptionType);
            return GenericResult<DataOptionOutcome>.Failure(
                DataOptionResultCodes.ByName("DataOptionTypeNotFound"),
                ResultDetails.Create("OptionType", context.OptionType));
        }
    }
}
