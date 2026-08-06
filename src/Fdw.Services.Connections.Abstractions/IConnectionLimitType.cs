using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Marker interface for connection limit TypeOptions.
/// Each TypeOption represents a distinct class of outbound protection
/// (rate, concurrency, size, timeout, daily budget).
///
/// Connection-type packages define their own TypeCollection that implements this interface
/// (e.g., MsSqlConnectionLimitTypes, HttpConnectionLimitTypes).
/// The runtime enforcement layer uses only this base interface, keeping it invisible
/// to the per-connection-type packages.
/// </summary>
public interface IConnectionLimitType : ITypeOption<int, ConnectionLimitTypeBase>
{
    /// <summary>
    /// Declarative field descriptors for this limit type's configuration properties.
    /// Used by the UI to render per-type editors without per-type markup.
    /// </summary>
    IReadOnlyList<ConfigurationFieldDescriptor> ConfigurationFields { get; }
}
