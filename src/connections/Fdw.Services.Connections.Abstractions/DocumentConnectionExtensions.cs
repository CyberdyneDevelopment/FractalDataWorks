using System;
using System.Linq;
using Fdw.Documents.Abstractions.Capabilities;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>Asks a document connection what it can do (FDW-1499).</summary>
public static class DocumentConnectionExtensions
{
    /// <summary>Says whether the connection declares a capability.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="capability">The capability asked for.</param>
    /// <returns>True when the connection declares it.</returns>
    public static bool Supports(this IDocumentConnection connection, IDocumentCapability capability)
    {
        if (connection is null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (capability is null)
        {
            throw new ArgumentNullException(nameof(capability));
        }

        return connection.Capabilities.Any(declared => string.Equals(declared.Name, capability.Name, StringComparison.Ordinal));
    }
}
