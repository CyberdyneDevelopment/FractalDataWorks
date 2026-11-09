using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions.Authentication;

/// <summary>
/// What every connection kind's authentication method states about the values a connection holds for it, so the connection endpoints
/// treat those values the same way for every kind (FDW-4607).
/// </summary>
public interface IConnectionAuthenticationType
{
    /// <summary>Gets the method's name; empty for the sentinel returned for a name no option answers to.</summary>
    string Name { get; }

    /// <summary>Gets the authentication values this method accepts.</summary>
    IReadOnlyList<string> ExpectedProperties { get; }

    /// <summary>Gets the names in <paramref name="names"/> that this method does not expect; never their values.</summary>
    /// <param name="names">The names of the connection's authentication values.</param>
    IReadOnlyList<string> UnexpectedProperties(IEnumerable<string> names);

    /// <summary>Masks the stored-credential references and every unexpected key in <paramref name="values"/>.</summary>
    /// <param name="values">The connection's authentication values.</param>
    /// <param name="maskValue">What a masked value is shown as.</param>
    IDictionary<string, string?> MaskSecrets(IReadOnlyDictionary<string, string?> values, string maskValue);
}
