using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// One option type of the DataOptions type collection (FDW-5665) and the options a person can pick for it: the names a stored
/// option row states as its <c>OptionType</c> and <c>Option</c>.
/// </summary>
public sealed class DataOptionTypePayload
{
    /// <summary>Gets or sets the option type's name, for example <c>SelectList</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the names of the type's options, in the order they apply (for SelectList, <c>Include</c> then
    /// <c>Exclude</c>). Empty when the type registers none.
    /// </summary>
    public List<string> Options { get; set; } = [];
}
