using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in capability interface for connection types that declare which write modes they support.
/// </summary>
/// <remarks>
/// Connection types implement this interface to enumerate the write strategies available
/// (e.g. Append, Overwrite, Upsert, TruncateInsert).
/// Consistent with the <c>ISupportsCalculationPushdown</c> capability pattern.
/// </remarks>
public interface ISupportsWriteModes
{
    /// <summary>
    /// Gets the write mode names supported by this connection type.
    /// </summary>
    IReadOnlyList<string> SupportedWriteModes { get; }
}
