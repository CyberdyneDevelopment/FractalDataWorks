using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Authentication.AzureAd;

/// <summary>An Entra ID mode SqlClient takes (<c>AzureAdMode</c>) and the authentication fragment of the connection string it builds.</summary>
public interface IAzureAdMode : ITypeOption<int, IAzureAdMode>
{
    /// <summary>Gets the word that names the mode, in upper case (<c>SPN</c>).</summary>
    [TypeLookup("ByWord")]
    string Word { get; }

    /// <summary>Builds the authentication fragment of the connection string for this mode.</summary>
    /// <param name="values">The connection's authentication values.</param>
    /// <param name="resolvedPassword">The resolved secret, when the mode signs in with one.</param>
    /// <returns>The fragment.</returns>
    IGenericResult<string> Fragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword);
}
