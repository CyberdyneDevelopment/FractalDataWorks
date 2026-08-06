using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in capability interface for connection types that declare which data path format templates they support.
/// </summary>
/// <remarks>
/// Connection types implement this interface to enumerate the path format patterns used to address
/// containers (e.g. "{schema}.{table}", "{schema}.{storedprocedure}").
/// Consistent with the <c>ISupportsCalculationPushdown</c> capability pattern.
/// </remarks>
public interface ISupportsDataPathFormats
{
    /// <summary>
    /// Gets the path format templates supported by this connection type.
    /// </summary>
    IReadOnlyList<string> SupportedPathFormats { get; }
}
