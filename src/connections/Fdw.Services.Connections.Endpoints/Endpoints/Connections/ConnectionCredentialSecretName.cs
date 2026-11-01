using System;
using System.Globalization;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Connections.Results;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// The one definition of the name a connection credential is stored under in the credential secret manager (FDW-1185).
/// </summary>
/// <remarks>
/// <para>
/// A name is the connection prefix, a fresh id and the credential field's name, joined by hyphens:
/// <c>Connection-{id}-{field}</c>. Hyphens, letters and digits are the only characters every secret manager accepts (Azure
/// Key Vault refuses a dot, an underscore or anything else with a <c>BadParameter</c> answer), and a name may not pass
/// <see cref="MaxLength"/> characters.
/// </para>
/// <para>
/// Nothing reads a secret's value by parsing its name. A connection keeps the full name it was given and the secret
/// manager looks it up by that name, so a credential stored under an older name (<c>Connection.{id}.{field}</c>) is still
/// read. The one thing the shape decides is ownership (<see cref="IsMinted"/>): the store deletes only the secrets it
/// minted, and leaves any other name a connection points at.
/// </para>
/// </remarks>
public static class ConnectionCredentialSecretName
{
    /// <summary>The longest secret name every secret manager accepts, in characters.</summary>
    public const int MaxLength = 127;

    private const string Prefix = "Connection";
    private const char Separator = '-';
    private const char OlderSeparator = '.';
    private const int IdLength = 32;

    /// <summary>Mints a fresh secret name for one credential field of a connection.</summary>
    /// <param name="connectionName">The connection the credential belongs to, for the refusal.</param>
    /// <param name="field">The credential field the secret holds, as the connection type declares it.</param>
    /// <returns>The name, or a <c>CredentialFieldNameRefused</c> failure when the field cannot be part of a storable name.</returns>
    public static IGenericResult<string> Mint(string connectionName, string field)
    {
        var name = string.Create(CultureInfo.InvariantCulture, $"{Prefix}{Separator}{Guid.NewGuid():N}{Separator}{field}");
        return IsStorable(name)
            ? GenericResult<string>.Success(name)
            : GenericResult<string>.Failure(
                ConnectionsResultCodes.ByName("CredentialFieldNameRefused"),
                ResultDetails.Create("name", connectionName, "field", field).With("limit", MaxLength.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Tells whether the credential store minted a secret name, so the store may delete the secret it holds.</summary>
    /// <param name="name">The secret name.</param>
    /// <returns>
    /// <see langword="true"/> for <c>Connection-{id}-{field}</c> and the older <c>Connection.{id}.{field}</c>, where
    /// <c>{id}</c> is a lowercase 32-digit hexadecimal id; <see langword="false"/> for any other name, such as a secret an
    /// operator keeps and a connection only points at.
    /// </returns>
    public static bool IsMinted(string name) =>
        name.StartsWith(Prefix, StringComparison.Ordinal)
        && name.Length > Prefix.Length + IdLength + 2
        && name[Prefix.Length] is Separator or OlderSeparator
        && name[Prefix.Length + 1 + IdLength] == name[Prefix.Length]
        && name.Skip(Prefix.Length + 1).Take(IdLength).All(IsLowercaseHex);

    /// <summary>Tells whether a secret name uses only characters every secret manager accepts and fits the length limit.</summary>
    /// <param name="name">The secret name.</param>
    /// <returns><see langword="true"/> when every secret manager would accept the name.</returns>
    public static bool IsStorable(string name) => name.Length > 0 && name.Length <= MaxLength && name.All(IsPermitted);

    private static bool IsLowercaseHex(char character) => character is >= '0' and <= '9' or >= 'a' and <= 'f';

    private static bool IsPermitted(char character) =>
        character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or Separator;
}
