using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.Options;

/// <summary>
/// One option type's entry point into the <see cref="DataOptions"/> root collection.
/// </summary>
/// <remarks>
/// A root member is a dispatcher, not an option: <see cref="ByName"/> resolves an option name against that option type's own
/// child collection (for example <c>SelectListOptions</c>). Resolve a stored row's option as
/// <c>DataOptions.ByName(optionType).ByName(option)</c>.
/// </remarks>
public interface IDataOptionType : ITypeOption<int, DataOptionTypeBase>
{
    /// <summary>Resolves an option name against this option type's own option collection.</summary>
    /// <param name="optionName">The option name, for example <c>"Include"</c>.</param>
    /// <returns>The matching option, or an option whose <see cref="IDataOption.Apply"/> fails because the name is not known.</returns>
    IDataOption ByName(string optionName);

    /// <summary>
    /// Gets every option registered for this option type, which is what a person can pick from. Empty when the option type
    /// registers none.
    /// </summary>
    IReadOnlyList<IDataOption> Options { get; }
}
