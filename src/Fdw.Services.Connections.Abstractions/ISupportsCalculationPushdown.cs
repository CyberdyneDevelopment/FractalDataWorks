using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in capability interface for connection types that support SQL calculation pushdown.
/// </summary>
/// <remarks>
/// Connection types implement this interface to declare which calculation names they can
/// execute natively via SQL rather than requiring in-memory evaluation.
/// Consistent with the <c>ISchemaDiscovery</c> capability pattern.
/// </remarks>
public interface ISupportsCalculationPushdown
{
    /// <summary>
    /// Gets the calculation names (matching <c>CalculationTypes</c> or <c>WindowedCalculationTypes</c> names)
    /// that this connection type can execute as SQL expressions.
    /// </summary>
    IReadOnlyList<string> SupportedCalculations { get; }
}
