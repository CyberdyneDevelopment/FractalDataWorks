using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// One authentication method a connection type offers, with the credentials it asks for (FDW-1090).
/// </summary>
public sealed class ConnectionAuthenticationTypeInfo
{
    /// <summary>Gets or sets the method's name, as a connection's <c>authenticationType</c> names it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the method's display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the method, with none of its own options set, acts as the host; creating
    /// or changing such a connection is a platform administrator's decision (FDW-1068).
    /// </summary>
    public bool UsesHostIdentity { get; set; }

    /// <summary>Gets or sets the credentials the method asks for; empty for a method that carries none.</summary>
    public IReadOnlyList<ConnectionCredentialFieldInfo> CredentialFields { get; set; } = [];

    /// <summary>
    /// Gets or sets the non-secret values the method requires besides its credentials (a SQL login's user name); empty
    /// for a method that needs none (FDW-1336). Each is sent in <c>authentication</c> under its field name.
    /// </summary>
    public IReadOnlyList<ConnectionSettingFieldInfo> Settings { get; set; } = [];

    /// <summary>Describes an authentication method for a form.</summary>
    /// <param name="method">The method, as its connection type describes it.</param>
    /// <returns>The description.</returns>
    public static ConnectionAuthenticationTypeInfo Of(ConnectionAuthenticationMethod method)
    {
        if (method is null) throw new System.ArgumentNullException(nameof(method));
        return new()
        {
            Name = method.Name,
            DisplayName = method.DisplayName,
            UsesHostIdentity = method.UsesHostIdentity,
            CredentialFields = method.Credentials
                .Select(credential => new ConnectionCredentialFieldInfo
                {
                    Field = credential.Field,
                    Label = credential.Label,
                    // Why always true: the credential store takes all of a method's credentials or none
                    // (ConnectionCredentialStore.CheckFields), so none of them is optional.
                    Required = true,
                    Secret = credential.Secret,
                })
                .ToList(),
            Settings = method.Settings
                .Select(setting => new ConnectionSettingFieldInfo { Field = setting.Field, Label = setting.Label, Required = true })
                .ToList(),
        };
    }
}
