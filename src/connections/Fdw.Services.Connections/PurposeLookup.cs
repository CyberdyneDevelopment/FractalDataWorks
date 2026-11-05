using System;
using System.Linq;
using Fdw.Services.Connections.Abstractions.Purposes;

namespace Fdw.Services.Connections;

/// <summary>
/// Finds a purpose by the name a grant or an assignment stores (FDW-3673), compared without regard to case, as grants and assignments compare
/// purposes everywhere else.
/// </summary>
internal static class PurposeLookup
{
    /// <summary>Finds a purpose by name.</summary>
    /// <param name="purpose">The name.</param>
    /// <returns>The purpose, or null when no purpose has the name.</returns>
    public static IConnectionPurpose? Named(string purpose) =>
        ConnectionPurposes.All().FirstOrDefault(option => string.Equals(option.Name, purpose, StringComparison.OrdinalIgnoreCase));

    /// <summary>Names every purpose, for a refusal that lists them.</summary>
    /// <returns>The purposes' names.</returns>
    public static string Names() => string.Join(", ", ConnectionPurposes.All().Select(option => option.Name));
}
