using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// An option that reserves columns of a destination for the engine to fill in (FDW-1148).
/// </summary>
/// <remarks>
/// The names are data: they are read from the option, not written into the validator or the console, and they are listed in
/// <c>/type-collections/LoadPhases/values</c> as <c>reservedColumns</c> so a form can start a reject step with them and say
/// when a data set lacks them. The engine matches them to a destination's columns by name.
/// </remarks>
public interface IReservedColumnsShape
{
    /// <summary>Gets the names of the columns the engine fills in, or an empty list when the option reserves none.</summary>
    IReadOnlyList<string> ReservedColumns { get; }
}
